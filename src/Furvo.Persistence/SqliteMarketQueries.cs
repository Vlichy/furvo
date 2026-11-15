using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Core.Queries;
namespace Furvo.Persistence;
/// <summary>Lectura global por lotes de la carrera activa. No inicializa cantera, migra ni escribe.</summary>
public sealed class SqliteMarketQueries(string path)
{
    public MarketSnapshot Read()
    {
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);return Read(c,t,false);
    }
    // La negociación necesita participantes propios para intercambios; la API pública de Mercado nunca los incluye.
    internal static MarketSnapshot ReadParticipants(Microsoft.Data.Sqlite.SqliteConnection c,Microsoft.Data.Sqlite.SqliteTransaction t)=>Read(c,t,true);
    private static MarketSnapshot Read(Microsoft.Data.Sqlite.SqliteConnection c,Microsoft.Data.Sqlite.SqliteTransaction t,bool participants)
    {
        var owner=SqliteLineupStore.Owner(c,t);var world=SqliteWorldStore.Load(c,t);
        var clubs=new WorldQueries(world,owner.Date).GetClubs();var clubById=clubs.ToDictionary(p=>p.Id);var players=world.Players.ToDictionary(p=>p.Id);var memberships=new Dictionary<PlayerId,(ClubId Club,Guid Team)>();
        var teams=clubs.ToDictionary(p=>p.Id.Value,p=>(Club:p.Id,Name:p.Name,Category:"First"));
        if(SqliteAcademyStore.HasSchema(c,t)){
            using(var q=WorldDatabase.Command(c,t,"SELECT id,club_id,name,category FROM academy_teams ORDER BY id"))using(var r=q.ExecuteReader())while(r.Read())teams.Add(Guid.Parse(r.GetString(0)),(new ClubId(Guid.Parse(r.GetString(1))),r.GetString(2),r.GetString(3)));
            using var q2=WorldDatabase.Command(c,t,"SELECT club_id,team_id,data_json FROM academy_players ORDER BY id");using var rows=q2.ExecuteReader();while(rows.Read()){var p=SqliteAcademyStore.DecodePlayer(rows.GetString(2));if(!players.TryAdd(p.Id,p))throw new InvalidDataException("Jugador duplicado entre mundo y cantera.");memberships[p.Id]=(new ClubId(Guid.Parse(rows.GetString(0))),Guid.Parse(rows.GetString(1)));}
        }
        var contracts=world.Contracts.GroupBy(p=>p.PlayerId).ToDictionary(g=>g.Key,g=>g.OrderByDescending(p=>p.StartDate).ThenBy(p=>p.Id.ToString(),StringComparer.Ordinal).ToArray());
        foreach(var p in world.Players){var active=contracts.GetValueOrDefault(p.Id)?.SingleOrDefault(k=>k.StartDate<=owner.Date&&k.EndDate>=owner.Date);if(active is not null)memberships[p.Id]=(active.ClubId,active.ClubId.Value);}
        if(SqliteSportsStore.HasSchema(c,t)){
            using(var q=WorldDatabase.Command(c,t,"SELECT player_id,club_id,team_id FROM sport_assignments ORDER BY player_id"))using(var rows=q.ExecuteReader())while(rows.Read()){
                var id=new PlayerId(Guid.Parse(rows.GetString(0)));var club=new ClubId(Guid.Parse(rows.GetString(1)));var team=Guid.Parse(rows.GetString(2));if(!players.ContainsKey(id)||!teams.TryGetValue(team,out var target)||target.Club!=club||memberships.TryGetValue(id,out var old)&&old.Club!=club)throw new InvalidDataException("Asignación deportiva incompatible.");memberships[id]=(club,team);
            }
            using var q2=WorldDatabase.Command(c,t,"SELECT player_id,data_json FROM training_development ORDER BY player_id");using var reader=q2.ExecuteReader();while(reader.Read()){
                var id=new PlayerId(Guid.Parse(reader.GetString(0)));if(!players.TryGetValue(id,out var player))continue;var state=CareerJson.Read<DevelopmentState>(reader.GetString(1));var values=player.Attributes.AllValues.ToDictionary();foreach(var change in state.AttributeChanges)if(values.ContainsKey(change.Key)&&!PlayerAttributeCatalog.Get(change.Key).IsHidden)values[change.Key]=Math.Clamp(values[change.Key]+change.Value,1,20);
                players[id]=new(player.Id,player.Name,player.BirthDate,player.Nationality,player.MainPosition,new PlayerAttributes(values),player.Positions);
            }
        }
        var result=new List<MarketPlayer>();foreach(var p in players.Values.OrderBy(p=>p.Id.ToString(),StringComparer.Ordinal)){
            ClubSummary? club=null;Guid? team=null;string? name=null;string? category=null;if(memberships.TryGetValue(p.Id,out var member)){if(!teams.TryGetValue(member.Team,out var metadata)||metadata.Club!=member.Club)throw new InvalidDataException("Equipo del jugador desconocido.");club=clubById[member.Club];team=member.Team;name=metadata.Name;category=metadata.Category;}
            // Se excluye el club matriz completo antes de proyectar atributos o fichas públicas.
            // Sin pertenencia actual verificada tampoco se afirma que sea un jugador de otro club.
            if(!participants&&(club is null||club.Id==owner.Club))continue;
            var records=contracts.GetValueOrDefault(p.Id)??Array.Empty<Contract>();var contract=records.FirstOrDefault(k=>k.StartDate<=owner.Date&&k.EndDate>=owner.Date)??records.FirstOrDefault();
            var projected=contract is null?null:new MarketContract(contract.Id,clubById[contract.ClubId],contract.StartDate,contract.EndDate,contract.StartDate is null||contract.EndDate is null?null:contract.StartDate<=owner.Date&&contract.EndDate>=owner.Date,ContractInformation.Release(contract,new(owner.Date,owner.Club,world.Clubs.Single(c=>c.Id==contract.ClubId).Country,contract.ClubId==owner.Club?null:world.Clubs.FirstOrDefault(c=>c.Id==owner.Club)?.Country,BuyerClub:contract.ClubId==owner.Club?null:owner.Club)));
            result.Add(new(WorldQueries.ProjectPlayer(p,club,owner.Date),team,name,category,projected));
        }
        return new(owner.Save.Value,owner.Date,clubs,result.AsReadOnly(),owner.Club);
    }
}
