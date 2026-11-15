using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Core.Academy;
using Furvo.Core.Development;
using Furvo.Core.Tactics;
using Furvo.Core.Lineups;
using Furvo.Core.Queries;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Solo la autoridad principal acepta propuestas. La normativa se vuelve a consultar dentro de la transacción.</summary>
public sealed class SqliteAcademyMobilityStore(string path,IAcademyEligibility? eligibility=null)
{
    private readonly IAcademyEligibility _eligibility=eligibility??new PendingAcademyEligibility();
    public EligibilityDecision Check(MobilityRequest request)
    {
        var squads=new SqliteClubSquadStore(path);var teams=squads.Teams();var all=squads.All();if(request.Source==request.Destination||!all.TryGetValue(request.Player,out var actor)||actor.Team!=request.Source||teams.All(t=>t.Id!=request.Destination))return new(false,new[]{"Jugador, origen o destino ajeno al club o desactualizado."},"identity");
        if(request.Kind==MobilityKind.Temporary)return new(false,new[]{"Convocatoria temporal pendiente de calendario, encuentro e inscripción verificables."},"match-context-pending");
        var destination=teams.Single(t=>t.Id==request.Destination);var world=new SqliteWorldStore(path).Load();var nation=world.Clubs.Single(c=>c.Id==destination.Club).Country;var country=nation is "España" or "Spain" or "ES"?"ES":nation??"";return _eligibility.Check(new(destination.Club,actor.Player,request.Source,request.Destination,destination.Category,destination.Competition,request.Date,all.Values.Count(p=>p.Team==destination.Id),country,country=="ES"?"RFEF":"",world.Seasons.Single(s=>s.StartDate<=request.Date&&s.EndDate>=request.Date).Name??""),request.Kind);
    }
    public IReadOnlyList<MovementRecommendation> Recommendations()
    {using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);_=SqliteLineupStore.Owner(c,t);if(!SqliteSportsStore.HasSchema(c,t))return Array.Empty<MovementRecommendation>();using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM movement_recommendations ORDER BY id");using var r=q.ExecuteReader();var list=new List<MovementRecommendation>();while(r.Read())list.Add(CareerJson.Read<MovementRecommendation>(r.GetString(0)));return list;}
    /// <summary>Consulta cronológica del registro existente; no comprueba ni ejecuta una nueva operación.</summary>
    public IReadOnlyList<AcademyMovementRecord> History(PlayerId player)
    {
        if(!new SqliteClubSquadStore(path).All(false).ContainsKey(player))throw new InvalidOperationException("Futbolista ajeno al club dirigido.");
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);_=SqliteLineupStore.Owner(c,t);if(!SqliteSportsStore.HasSchema(c,t))return Array.Empty<AcademyMovementRecord>();
        using var q=WorldDatabase.Command(c,t,"SELECT id,occurred_on,data_json,evidence FROM movement_history WHERE player_id=$player ORDER BY occurred_on,id",("$player",player.ToString()));using var r=q.ExecuteReader();var result=new List<AcademyMovementRecord>();while(r.Read())result.Add(new(r.GetInt64(0),DateOnly.ParseExact(r.GetString(1),"yyyy-MM-dd"),CareerJson.Read<MobilityRequest>(r.GetString(2)),r.GetString(3)));return result;
    }
    public void Recommend(MovementRecommendation proposal)
    {
        var coach=new SqliteAcademyStore(path).Get(proposal.Source).Coach;var all=new SqliteClubSquadStore(path).All();var teams=new SqliteClubSquadStore(path).Teams();
        if(proposal.Id==Guid.Empty||proposal.Status!=RecommendationStatus.Pending||proposal.Coach!=coach.Id||string.IsNullOrWhiteSpace(proposal.Reason)||proposal.Decision is not null||proposal.Date!=new SqliteSportsStore(path).CareerDate()||!all.TryGetValue(proposal.Player,out var player)||player.Team!=proposal.Source||proposal.Destination==proposal.Source||teams.All(t=>t.Id!=proposal.Destination))throw new InvalidDataException("Propuesta sin entrenador responsable, jugador o destino válido.");
        if(proposal.RelevantAttributes?.Any(a=>PlayerAttributeCatalog.Get(a.Key).IsHidden||player.Player.Attributes.VisibleValues.GetValueOrDefault(a.Key)!=a.Value)==true)throw new InvalidDataException("Los atributos del informe deben ser visibles y corresponder al jugador.");var decision=Check(new(proposal.Player,proposal.Source,proposal.Destination,proposal.Kind,proposal.Date));proposal=proposal with{Age=player.Player.AgeOn(proposal.Date),RegulatorySummary=decision.Allowed?"Comprobaciones disponibles superadas.":string.Join(" ",decision.Reasons),Readiness="Sin evaluación de rendimiento competitivo disponible."};
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();SqliteSportsStore.Ensure(c,t);WriteProposal(c,t,proposal,true);t.Commit();
    }
    private static void WriteProposal(SqliteConnection c,SqliteTransaction t,MovementRecommendation value,bool insert=false)
    {using var q=WorldDatabase.Command(c,t,insert?"INSERT INTO movement_recommendations VALUES($id,$json);":"UPDATE movement_recommendations SET data_json=$json WHERE id=$id;",("$id",value.Id.ToString("D")),("$json",CareerJson.Write(value)));if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("La propuesta cambió.");}
    public void Reject(Guid proposal,Guid principal)
    {
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();RequirePrincipal(c,t,principal);var p=ReadProposal(c,t,proposal);if(p.Status!=RecommendationStatus.Pending)throw new InvalidOperationException("La propuesta ya está resuelta.");WriteProposal(c,t,p with{Status=RecommendationStatus.Rejected,Decision="Rechazada por el entrenador principal."});t.Commit();
    }
    public void Accept(Guid proposal,Guid principal)
    {var p=Recommendations().Single(r=>r.Id==proposal);Move(new(p.Player,p.Source,p.Destination,p.Kind,new SqliteSportsStore(path).CareerDate()),principal,proposal);}
    private static MovementRecommendation ReadProposal(SqliteConnection c,SqliteTransaction t,Guid id)
    {using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM movement_recommendations WHERE id=$id",("$id",id.ToString("D")));return q.ExecuteScalar() is string json?CareerJson.Read<MovementRecommendation>(json):throw new InvalidOperationException("Propuesta inexistente.");}
    private static void RequirePrincipal(SqliteConnection c,SqliteTransaction t,Guid principal)
    {var owner=SqliteLineupStore.Owner(c,t);using var q=WorldDatabase.Command(c,t,"SELECT coach_id FROM career_state WHERE singleton=1");if(owner.Club is null||q.ExecuteScalar() as string!=principal.ToString("D"))throw new InvalidOperationException("Solo el entrenador principal puede decidir movimientos.");}
    public void Move(MobilityRequest request,Guid principal,Guid? proposalId=null)
    {
        var squads=new SqliteClubSquadStore(path);var teams=squads.Teams();var all=squads.All(false);var decision=Check(request);if(!decision.Allowed)throw new InvalidOperationException(string.Join(" ",decision.Reasons));
        var ownerTeam=teams[0];var date=new SqliteSportsStore(path).CareerDate();if(request.Date!=date)throw new InvalidDataException("El movimiento requiere la fecha real de la carrera.");var player=all[request.Player].Player;
        if(all.Values.Count(p=>p.Team==request.Source)<=11)throw new InvalidOperationException("El equipo de origen quedaría sin once disponible.");
        var parent=new SqliteTacticalPlanStore(path);var primary=parent.Load()??throw new InvalidOperationException("Falta táctica principal.");var common=parent.Settings(primary,TacticalPlanKind.Initial);var academy=new SqliteAcademyStore(path);var states=academy.List().Where(s=>s.Team.Id==request.Source||s.Team.Id==request.Destination).ToArray();
        all=all.ToDictionary(p=>p.Key,p=>p.Key==request.Player?(p.Value.Player,request.Destination):p.Value);
        IReadOnlyList<LineupPlayer> Actors(Guid team)=>all.Values.Where(p=>p.Team==team).Select(p=>LineupPlayer.From(WorldQueries.ProjectPlayer(p.Player,new(ownerTeam.Club,teams.Single(t=>t.Id==team).Name,null,null),date))).ToArray();
        TacticalPlanBook Reassign(TacticalPlanBook b,Guid team)=>b with{Plans=b.Plans.Select(p=>p with{Roster=AcademyTactics.Reconcile(b.ClubId,p.Roster,p.WithoutBall.Formation,Actors(team),p.WithoutBall.Positions.ToDictionary(s=>s.Id,s=>s.Role))}).ToArray()};
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();RequirePrincipal(c,t,principal);SqliteSportsStore.Ensure(c,t);
        // La política se revalida con la operación actual y no solo con el informe previo.
        decision=Check(request);if(!decision.Allowed)throw new InvalidOperationException(string.Join(" ",decision.Reasons));
        MovementRecommendation? proposal=null;if(proposalId is {} id){proposal=ReadProposal(c,t,id);if(proposal.Status!=RecommendationStatus.Pending||proposal.Player!=request.Player||proposal.Source!=request.Source||proposal.Destination!=request.Destination||proposal.Kind!=request.Kind)throw new InvalidOperationException("La propuesta ya no corresponde a esta operación.");}
        using(var assign=WorldDatabase.Command(c,t,"INSERT INTO sport_assignments VALUES($player,$club,$team) ON CONFLICT(player_id) DO UPDATE SET team_id=excluded.team_id WHERE club_id=excluded.club_id;",("$player",player.Id.ToString()),("$club",ownerTeam.Club.ToString()),("$team",request.Destination.ToString("D"))))if(assign.ExecuteNonQuery()!=1)throw new InvalidOperationException("Propiedad incompatible.");
        if(request.Destination==ownerTeam.Id){using var remove=WorldDatabase.Command(c,t,"DELETE FROM academy_players WHERE id=$id;",("$id",player.Id.ToString()));if(remove.ExecuteNonQuery()==1)SqliteWorldStore.Write(c,t,new WorldData(players:new[]{player}));}
        else {using var move=WorldDatabase.Command(c,t,"UPDATE academy_players SET team_id=$team WHERE id=$id AND club_id=$club",("$team",request.Destination.ToString("D")),("$id",player.Id.ToString()),("$club",ownerTeam.Club.ToString()));move.ExecuteNonQuery();}
        var current=SqliteTacticalPlanStore.ReadPersisted(c,t);if(current is not null&&current.Revision!=primary.Revision)throw new InvalidOperationException("Las tácticas principales cambiaron.");
        var nextPrimary=Reassign(primary,ownerTeam.Id);TacticalPlanRules.Validate(nextPrimary,Actors(ownerTeam.Id));var raw=SqliteTacticsStore.ReadRaw(c,t)??TacticalSettings.Undefined(ownerTeam.Club);var nextCommon=FilterPersonnel(common,Actors(ownerTeam.Id).Select(p=>p.Id).ToHashSet()) with{Revision=raw.Revision,Mentality=null,Instructions=TacticalSettings.Undefined(ownerTeam.Club).Instructions};
        SqliteTacticalPlanStore.WriteAtomic(c,t,nextPrimary,nextCommon);
        foreach(var state in states){var roster=Actors(state.Team.Id);var book=state.TacticMode==AcademyTacticMode.Own?Reassign(state.OwnBook!,state.Team.Id):null;var basis=book??nextPrimary;
            var assignments=Enum.GetValues<TacticalPlanKind>().ToDictionary(k=>k,k=>new AcademyAssignments(AcademyTactics.Reconcile(state.Team.ClubId,state.Rosters[k].Roster,basis.Get(k).WithoutBall.Formation,roster,state.Rosters[k].Roles),basis.Get(k).WithoutBall.Positions.ToDictionary(s=>s.Id,s=>s.Role)));
            var next=state with{Revision=state.Revision+1,Rosters=assignments,OwnBook=book is null?null:book with{Revision=state.Revision+1},Common=FilterPersonnel(state.Common,roster.Select(p=>p.Id).ToHashSet())};SqliteAcademyStore.Write(c,t,next,state.Revision);}
        using(var history=WorldDatabase.Command(c,t,"INSERT INTO movement_history(player_id,occurred_on,data_json,evidence) VALUES($player,$date,$json,$evidence)",("$player",player.Id.ToString()),("$date",date.ToString("yyyy-MM-dd")),("$json",CareerJson.Write(request)),("$evidence",decision.EvidenceVersion)))history.ExecuteNonQuery();
        // Las sesiones futuras de los equipos afectados se conservan para revisión, sin ejecutar ni recalcular cargas.
        using(var invalidate=WorldDatabase.Command(c,t,"UPDATE training_plans SET revision=revision+1,data_json=json_set(data_json,'$.Revision',revision+1,'$.RequiresReview',json('true')) WHERE team_id IN($source,$destination);",("$source",request.Source.ToString("D")),("$destination",request.Destination.ToString("D"))))invalidate.ExecuteNonQuery();
        if(proposal is not null)WriteProposal(c,t,proposal with{Status=RecommendationStatus.Accepted,Decision="Movimiento aprobado y ejecutado tras revalidación."});t.Commit();
    }
    private static TacticalSettings FilterPersonnel(TacticalSettings s,IReadOnlySet<PlayerId> squad)
    {
        var personnel=s.Personnel??TeamPersonnel.Empty();return s with{Personnel=personnel with{CaptainSlots=personnel.Captains.Select(p=>p is {} id&&squad.Contains(id)?p:null).ToArray(),Priorities=personnel.Priorities.ToDictionary(p=>p.Key,p=>(IReadOnlyList<PlayerId>)p.Value.Where(squad.Contains).ToArray())},PenaltyTakers=s.ShootoutOrder.Select(p=>p is {} id&&squad.Contains(id)?p:null).ToArray(),SetPieces=s.SetPieces.ToDictionary(p=>p.Key,p=>Clean(p.Value,squad)),FreeKicks=s.FreeKicks?.ToDictionary(p=>p.Key,p=>Clean(p.Value,squad))};
    }
    private static SetPiecePlan Clean(SetPiecePlan p,IReadOnlySet<PlayerId> squad)=>p with{Taker=p.Taker is {} id&&squad.Contains(id)?p.Taker:null,Details=p.Details is null?null:p.Details with{Assignments=p.Details.Assignments.ToDictionary(v=>v.Key,v=>v.Value is {} id&&squad.Contains(id)?v.Value:null)}};
}
