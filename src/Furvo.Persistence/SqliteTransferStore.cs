using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Core.Queries;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;

/// <summary>Registro transaccional de propuestas, sin pagos, cambios contractuales ni movimientos de plantilla.</summary>
public sealed class SqliteTransferStore(string path,TransferMoneyPolicy? moneyPolicy=null)
{
    public TransferMoneyPolicy MoneyPolicy {get;}=moneyPolicy??TransferMoneyPolicy.Default;
    private static bool Schema(SqliteConnection c,SqliteTransaction t)
    {
        using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='transfer_schema'");
        if(Convert.ToInt32(q.ExecuteScalar())==0)return false;q.CommandText="SELECT version FROM transfer_schema WHERE singleton=1";
        if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de Mercado incompatible.");return true;
    }
    private static IReadOnlyList<NegotiationVersion> Versions(SqliteConnection c,SqliteTransaction t,Guid? id=null)
    {
        var owner=SqliteLineupStore.Owner(c,t);if(!Schema(c,t))return Array.Empty<NegotiationVersion>();
        using var q=WorldDatabase.Command(c,t,"SELECT v.data_json FROM transfer_versions v JOIN transfer_negotiations n ON n.id=v.negotiation_id WHERE n.career_id=$career AND n.buyer_id=$buyer AND ($id IS NULL OR n.id=$id) ORDER BY n.id,v.revision",("$career",owner.Save.ToString()),("$buyer",owner.Club?.ToString()),("$id",id?.ToString("D")));
        using var r=q.ExecuteReader();var result=new List<NegotiationVersion>();while(r.Read())result.Add(CareerJson.Read<NegotiationVersion>(r.GetString(0)));return result;
    }
    public IReadOnlyList<NegotiationVersion> History(Guid? id=null)
    {using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);return Versions(c,t,id);}
    public IReadOnlyList<NegotiationVersion> Current()=>History().GroupBy(v=>v.Id).Select(g=>g.MaxBy(v=>v.Revision)!).OrderByDescending(v=>v.Date).ThenBy(v=>v.Id).ToArray();
    public IReadOnlyList<Competition> Competitions()=>new SqliteWorldStore(path).Load().Competitions;
    public MarketSnapshot Participants()
    {using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);return SqliteMarketQueries.ReadParticipants(c,t);}
    private static string Currency(SqliteConnection c,SqliteTransaction t,ClubId club)=>
        SqliteEconomyStore.ReadResources(c,t).FirstOrDefault(r=>r.ClubId==club)?.Currency??
        Furvo.Core.Careers.ContractCountryPolicies.For(SqliteWorldStore.Load(c,t).Clubs.Single(c=>c.Id==club).Country).Currency;
    public string Currency(){using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);return Currency(c,t,SqliteLineupStore.Owner(c,t).Club??throw new InvalidOperationException("Sin club dirigido."));}
    private static void ParticipantsValid(SqliteConnection c,SqliteTransaction t,NegotiationVersion version,bool sending,TransferMoneyPolicy policy,DateOnly proposalDate)
    {
        var data=SqliteMarketQueries.ReadParticipants(c,t);
        if(data.ManagedClub is null||data.Career!=version.Career||data.ManagedClub!=version.Buyer||version.Buyer==version.Seller)throw new InvalidOperationException("La propuesta no pertenece al club dirigido de esta carrera.");
        if(!data.Players.Any(p=>p.Details.Player.Id==version.Target&&p.Details.Club?.Id==version.Seller))throw new InvalidOperationException("El jugador solicitado ya no pertenece al vendedor.");
        foreach(var id in version.Terms.OfferedPlayers)if(!data.Players.Any(p=>p.Details.Player.Id==id&&p.Details.Club?.Id==version.Buyer))throw new InvalidOperationException("Un jugador incluido ya no pertenece al comprador.");
        version.Terms.Validate(data.Date,version.Seller,version.Target,sending,policy,proposalDate);
        var currency=Currency(c,t,version.Buyer);
        var competitions=SqliteWorldStore.Load(c,t).Competitions.Select(v=>v.Id.ToString()).ToHashSet();
        foreach(var bonus in version.Terms.Bonuses){
            if(!competitions.Contains(bonus.Scope)||bonus.Objective is BonusObjective.InternationalCaps or BonusObjective.EuropeanQualification or BonusObjective.CupTitle or BonusObjective.InternationalTitle)throw new InvalidDataException("No existe una referencia competitiva fiable para esta condición.");
            if(bonus.Objective==BonusObjective.CleanSheets&&data.Players.Single(p=>p.Details.Player.Id==version.Target).Details.Player.Position!=PlayerRole.Goalkeeper)throw new InvalidDataException("Las porterías a cero requieren un portero.");
        }
        if(currency is null||version.Terms.Currency!=currency)throw new InvalidOperationException("La moneda del club no está configurada o no coincide. No se realizan conversiones.");
    }
    private static void Add(SqliteConnection c,SqliteTransaction t,NegotiationVersion version,Guid operation)
    {
        using var q=WorldDatabase.Command(c,t,"INSERT INTO transfer_versions VALUES($id,$revision,$operation,$json)",("$id",version.Id.ToString("D")),("$revision",version.Revision),("$operation",operation.ToString("D")),("$json",CareerJson.Write(version)));q.ExecuteNonQuery();
    }
    private static NegotiationVersion? Replay(SqliteConnection c,SqliteTransaction t,Guid id,int expected,Guid operation,string action,TransferTerms? terms=null,PlayerId? target=null)
    {
        var owner=SqliteLineupStore.Owner(c,t);
        using var query=WorldDatabase.Command(c,t,"SELECT data_json FROM transfer_versions WHERE operation_id=$op",("$op",operation.ToString("D")));
        if(query.ExecuteScalar() is not string json)return null;
        var entry=CareerJson.Read<NegotiationVersion>(json);
        if(entry.Id!=id||entry.Career!=owner.Save.Value||entry.Buyer!=owner.Club||entry.Action!=action||
            entry.Revision-1!=expected||target is {} player&&player!=entry.Target||
            terms is not null&&CareerJson.Write(entry.Terms)!=CareerJson.Write(terms))
            throw new InvalidOperationException("Token de operación reutilizado con otra solicitud.");
        return entry.State==NegotiationState.AcceptedBetweenClubs
            ?Versions(c,t,id).FirstOrDefault(v=>v.Revision==entry.Revision+1&&v.State==NegotiationState.PendingPlayers)??entry
            :entry;
    }
    private NegotiationVersion Write(Guid id,int expected,Guid operation,string action,TransferTerms? terms,PlayerId? target,ClubDecision? response=null)
    {
        if(id==Guid.Empty||operation==Guid.Empty||expected<0)throw new InvalidDataException("Identidad o revisión de propuesta inválida.");
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var owner=SqliteLineupStore.Owner(c,t);
        if(owner.Club is null)throw new InvalidOperationException("No hay club dirigido.");
        if(!Schema(c,t))CareerJson.Migrate(c,t,"Transfer001_Negotiations.sql");
        var replay=Replay(c,t,id,expected,operation,action,terms,target);
        if(replay is not null)return replay;
        var old=Versions(c,t,id).LastOrDefault();
        if((old?.Revision??0)!=expected)throw new InvalidOperationException("La negociación cambió. Vuelve a abrirla.");
        NegotiationVersion next;
        if(old is null){
            if(action!="Draft"||target is null||terms is null)throw new InvalidOperationException("Primero debe existir un borrador.");
            var data=SqliteMarketQueries.ReadParticipants(c,t);var row=data.Players.SingleOrDefault(p=>p.Details.Player.Id==target&&p.Details.Club is {} externalClub&&externalClub.Id!=owner.Club)??throw new InvalidOperationException("Solo se proponen jugadores de clubes externos.");
            next=new(id,owner.Save.Value,owner.Club.Value,row.Details.Club!.Id,target.Value,1,owner.Date,NegotiationState.Draft,terms,action,null);
            using var head=WorldDatabase.Command(c,t,"INSERT INTO transfer_negotiations VALUES($id,$career,$buyer,$seller,$player)",("$id",id.ToString("D")),("$career",owner.Save.ToString()),("$buyer",next.Buyer.ToString()),("$seller",next.Seller.ToString()),("$player",next.Target.ToString()));head.ExecuteNonQuery();
        }else{
            if(target is {} changedTarget&&changedTarget!=old.Target)throw new InvalidOperationException("El futbolista solicitado no se cambia dentro de una negociación.");
            var state=action switch{"Draft"=>NegotiationState.Draft,"Send"=>NegotiationState.Sent,"Withdraw"=>NegotiationState.Withdrawn,"Cancel"=>NegotiationState.Cancelled,"Expire"=>NegotiationState.Expired,"AwaitResponse"=>NegotiationState.PendingResponse,"Response"=>response!.Kind switch{ClubDecisionKind.Counter=>NegotiationState.CounterReceived,ClubDecisionKind.Accept=>NegotiationState.AcceptedBetweenClubs,ClubDecisionKind.Reject=>NegotiationState.Rejected,_=>throw new InvalidDataException("Respuesta desconocida.")},_=>throw new InvalidOperationException("Acción no permitida.")};
            if(action=="Draft"&&old.State==NegotiationState.Draft){}else TransferRules.Transition(old.State,state);
            if(response is {} decision&&(decision.Negotiation!=id||decision.Revision!=old.Revision||decision.RespondingClub!=old.Seller||string.IsNullOrWhiteSpace(decision.Reason)))throw new InvalidOperationException("La respuesta no corresponde al vendedor y versión vigentes.");
            if(action=="Expire"&&owner.Date<=old.Terms.Expires)throw new InvalidOperationException("La propuesta todavía no ha caducado.");
            next=old with{Revision=checked(old.Revision+1),Date=owner.Date,State=state,Terms=terms??old.Terms,Action=action,Reason=response?.Reason??(action=="Withdraw"?"Retirada por el club comprador.":action=="Cancel"?"Cancelada por el club comprador.":action=="Expire"?"Plazo de propuesta superado.":null)};
        }
        // Retirar o cancelar debe seguir siendo posible aunque un participante ya no esté disponible.
        if(action is not("Withdraw" or "Cancel" or "Expire")){
            // Las revisiones sin cambios conservan la fecha en que se propusieron sus condiciones.
            var proposalDate=terms is not null?owner.Date:Versions(c,t,id).Last(v=>v.Action=="Draft"||v.State==NegotiationState.CounterReceived).Date;
            ParticipantsValid(c,t,next,action!="Draft",MoneyPolicy,proposalDate);
        }
        Add(c,t,next,operation);
        if(next.State==NegotiationState.AcceptedBetweenClubs){
            var pending=next with{Revision=next.Revision+1,State=NegotiationState.PendingPlayers,Action="AwaitPlayers"};
            TransferRules.Transition(next.State,pending.State);Add(c,t,pending,Guid.NewGuid());next=pending;
        }
        t.Commit();return next;
    }
    public NegotiationVersion Draft(Guid id,int revision,PlayerId player,TransferTerms terms,Guid operation)=>Write(id,revision,operation,"Draft",terms,player);
    public NegotiationVersion Send(Guid id,int revision,Guid operation)=>Write(id,revision,operation,"Send",null,null);
    public NegotiationVersion Expire(Guid id,int revision,Guid operation)=>Write(id,revision,operation,"Expire",null,null);
    public NegotiationVersion AwaitResponse(Guid id,int revision,Guid operation)=>Write(id,revision,operation,"AwaitResponse",null,null);
    public NegotiationVersion Withdraw(Guid id,int revision,Guid operation)=>Write(id,revision,operation,"Withdraw",null,null);
    public NegotiationVersion Cancel(Guid id,int revision,Guid operation)=>Write(id,revision,operation,"Cancel",null,null);
    public NegotiationVersion Respond(Guid id,int revision,Guid operation,IClubNegotiationDecisions source)
    {
        if(id==Guid.Empty||operation==Guid.Empty||revision<0)throw new InvalidDataException("Identidad o revisión de propuesta inválida.");
        NegotiationVersion proposal;
        using(var c=WorldDatabase.Open(path))
        using(var t=c.BeginTransaction(deferred:true)){
            if(!Schema(c,t))throw new InvalidOperationException("Negociación desconocida.");
            var replay=Replay(c,t,id,revision,operation,"Response");
            if(replay is not null)return replay;
            proposal=Versions(c,t,id).LastOrDefault()??throw new InvalidOperationException("Negociación desconocida.");
        }
        if(proposal.Revision!=revision)throw new InvalidOperationException("Revisión desactualizada.");
        var response=source.Decide(proposal);
        if(response.Kind==ClubDecisionKind.Counter&&response.CounterTerms is null||response.Kind!=ClubDecisionKind.Counter&&response.CounterTerms is not null)throw new InvalidDataException("Contenido de respuesta incompatible.");
        return Write(id,revision,operation,"Response",response.CounterTerms,null,response);
    }
}
