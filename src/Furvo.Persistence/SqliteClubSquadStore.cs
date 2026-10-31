using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Queries;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
public sealed record ClubTeam(Guid Id,ClubId Club,string Name,string Category,string? Competition);
public sealed record PublicPlayerLocation(PlayerDetails Details,Guid Team);
/// <summary>Una pertenencia deportiva. Las fuentes originales conservan identidad/contrato; la asignación de carrera decide la plantilla.</summary>
public sealed class SqliteClubSquadStore(string path)
{
    public IReadOnlyList<ClubTeam> Teams()
    {
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var club=SqliteLineupStore.Owner(c,t).Club??throw new InvalidOperationException("Sin club dirigido.");var world=new SqliteWorldStore(path).Load();
        return new[]{new ClubTeam(club.Value,club,world.Clubs.Single(x=>x.Id==club).Name,"First",null)}.Concat(new SqliteAcademyStore(path).List().Select(a=>new ClubTeam(a.Team.Id,club,a.Team.Name,a.Team.Category,a.Team.Competition))).ToArray();
    }
    public IReadOnlyDictionary<PlayerId,(Player Player,Guid Team)> All(bool applyDevelopment=true)
    {
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var owner=SqliteLineupStore.Owner(c,t);var club=owner.Club??throw new InvalidOperationException("Sin club dirigido.");var world=new SqliteWorldStore(path).Load();var q=new WorldQueries(world,owner.Date);var result=q.GetSquad(club).ToDictionary(p=>p.Id,p=>(world.Players.Single(x=>x.Id==p.Id),club.Value));
        if(SqliteAcademyStore.HasSchema(c,t)){using var a=WorldDatabase.Command(c,t,"SELECT team_id,data_json FROM academy_players WHERE club_id=$club;",("$club",club.ToString()));using var r=a.ExecuteReader();while(r.Read()){var p=SqliteAcademyStore.DecodePlayer(r.GetString(1));result.Add(p.Id,(p,Guid.Parse(r.GetString(0))));}}
        if(SqliteSportsStore.HasSchema(c,t)){using var a=WorldDatabase.Command(c,t,"SELECT player_id,team_id FROM sport_assignments WHERE club_id=$club;",("$club",club.ToString()));using var r=a.ExecuteReader();while(r.Read()){var id=new PlayerId(Guid.Parse(r.GetString(0)));var player=result.TryGetValue(id,out var old)?old.Item1:world.Players.Single(p=>p.Id==id);result[id]=(player,Guid.Parse(r.GetString(1)));}}
        if(applyDevelopment&&SqliteSportsStore.HasSchema(c,t)){using var d=WorldDatabase.Command(c,t,"SELECT player_id,data_json FROM training_development;");using var r=d.ExecuteReader();while(r.Read()){var id=new PlayerId(Guid.Parse(r.GetString(0)));if(!result.TryGetValue(id,out var item))continue;var state=CareerJson.Read<Furvo.Core.Development.DevelopmentState>(r.GetString(1));var values=item.Item1.Attributes.AllValues.ToDictionary();foreach(var change in state.AttributeChanges)if(values.ContainsKey(change.Key)&&!PlayerAttributeCatalog.Get(change.Key).IsHidden)values[change.Key]=Math.Clamp(values[change.Key]+change.Value,1,20);var p=item.Item1;result[id]=(new Player(p.Id,p.Name,p.BirthDate,p.Nationality,p.MainPosition,new PlayerAttributes(values),p.Positions),item.Item2);}}
        return result;
    }
    public IReadOnlyList<Player> Players(Guid team,bool applyDevelopment=true){if(!Teams().Any(t=>t.Id==team))throw new InvalidOperationException("Equipo ajeno al club.");return All(applyDevelopment).Values.Where(p=>p.Team==team).Select(p=>p.Player).OrderBy(p=>p.Id.ToString(),StringComparer.Ordinal).ToArray();}
    public IReadOnlyList<PlayerDetails> Details(Guid team)
    {var target=Teams().Single(t=>t.Id==team);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var date=SqliteLineupStore.Owner(c,t).Date;return Players(team).Select(p=>WorldQueries.ProjectPlayer(p,new(target.Club,target.Name,null,null),date)).ToArray();}
    public IReadOnlyList<LineupPlayer> LineupPlayers(Guid team)=>Details(team).Select(LineupPlayer.From).ToArray();
    public IReadOnlyList<Furvo.Core.Development.TrainingCandidate> Candidates(Guid team)=>Players(team).Select(p=>new Furvo.Core.Development.TrainingCandidate(p.Id,p.Name,p.BirthDate,p.Positions)).ToArray();
    public IReadOnlyDictionary<PlayerId,PublicPlayerLocation> Locations()=>Teams().SelectMany(t=>Details(t.Id).Select(p=>new PublicPlayerLocation(p,t.Id))).ToDictionary(p=>p.Details.Player.Id);
}
