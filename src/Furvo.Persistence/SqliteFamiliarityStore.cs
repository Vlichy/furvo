using Furvo.Core.Domain;
using Furvo.Core.Tactics;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Punto de integración para observaciones reales de futuros sistemas; ninguna pantalla genera aprendizaje.</summary>
public sealed class SqliteFamiliarityStore(string databasePath,Guid? academyTeam=null):IFamiliarityObservations
{
    private readonly string _path=WorldDatabase.PathFor(databasePath);
    private (Guid Team,IReadOnlySet<PlayerId> Squad) Scope()
    {
        if(academyTeam is {} team){var store=new SqliteAcademyStore(_path);store.Get(team);return(team,store.LineupPlayers(team).Select(p=>p.Id).ToHashSet());}
        var book=new SqliteTacticalPlanStore(_path).Load()??throw new InvalidOperationException("Sin equipo dirigido.");return(book.ClubId.Value,new SqliteLineupStore(_path).Players().Select(p=>p.Id).ToHashSet());
    }
    public IReadOnlyList<FamiliarityObservation> Read()
    {
        var scope=Scope();using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);
        using var exists=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='career_tactical_familiarity';");if(Convert.ToInt32(exists.ExecuteScalar())==0)return Array.Empty<FamiliarityObservation>();
        using var q=WorldDatabase.Command(c,t,"SELECT observation_json FROM career_tactical_familiarity WHERE team_id=$team ORDER BY player_id,aspect,configuration;",("$team",scope.Team.ToString("D")));using var r=q.ExecuteReader();var values=new List<FamiliarityObservation>();while(r.Read()){var value=CareerJson.Read<FamiliarityObservation>(r.GetString(0));value.Validate(scope.Squad);values.Add(value);}return values.AsReadOnly();
    }
    public void Record(FamiliarityObservation observation)
    {
        var scope=Scope();observation.Validate(scope.Squad);using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();
        if(observation.ObservedAt>SqliteLineupStore.Owner(c,t).Date)throw new InvalidDataException("La observación no puede proceder de una fecha futura.");
        SqliteTacticsStore.EnsureSchema(c,t);
        using var q=WorldDatabase.Command(c,t,"INSERT INTO career_tactical_familiarity VALUES($team,$player,$aspect,$configuration,$json) ON CONFLICT(team_id,player_id,aspect,configuration) DO UPDATE SET observation_json=excluded.observation_json;",("$team",scope.Team.ToString("D")),("$player",observation.Player.ToString()),("$aspect",(int)observation.Aspect),("$configuration",observation.Configuration),("$json",CareerJson.Write(observation)));q.ExecuteNonQuery();t.Commit();
    }
}
