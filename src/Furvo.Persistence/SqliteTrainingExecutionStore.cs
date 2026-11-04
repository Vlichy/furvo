using Furvo.Core.Domain;
using Furvo.Core.Development;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Ejecutor preparado para el futuro reloj de carrera. No existe botón que fabrique asistencias o avance el tiempo.</summary>
public sealed class SqliteTrainingExecutionStore(string path)
{
    /// <summary>Solo evidencias ya registradas: consultar el calendario no ejecuta sesiones ni crea el esquema.</summary>
    public IReadOnlySet<Guid> CompletedSessions(Guid team)
    {
        if(!new SqliteClubSquadStore(path).Teams().Any(t=>t.Id==team))throw new InvalidOperationException("Equipo ajeno.");
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);
        var result=new HashSet<Guid>();if(!SqliteSportsStore.HasSchema(c,t))return result;
        using var q=WorldDatabase.Command(c,t,"SELECT session_id FROM training_attendance WHERE team_id=$team;",("$team",team.ToString("D")));
        using var r=q.ExecuteReader();while(r.Read())result.Add(Guid.Parse(r.GetString(0)));return result;
    }
    public PhysicalObservation? ObservedPhysical(PlayerId player)=>State(player).Physical;
    public DevelopmentState State(PlayerId player)
    {using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var all=new SqliteClubSquadStore(path).All();if(!all.ContainsKey(player))throw new InvalidOperationException("Futbolista ajeno.");if(!SqliteSportsStore.HasSchema(c,t))return DevelopmentState.Empty(player);using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM training_development WHERE player_id=$id",("$id",player.ToString()));return q.ExecuteScalar() is string json?CareerJson.Read<DevelopmentState>(json):DevelopmentState.Empty(player);}
    public void Process(Guid team,DateOnly day,IReadOnlyList<TrainingEvidence> observations,IReadOnlyDictionary<PlayerId,PhysicalObservation> physical,ITrainingExecutionClock clock)
    {
        var service=new SqliteSportsStore(path);var plan=service.Plan(team);var squad=new SqliteClubSquadStore(path).Players(team,false);var completed=CompletedSessions(team);TrainingRules.Validate(plan with{Sessions=plan.Sessions.Where(s=>s.Date==day&&!completed.Contains(s.Id)).ToArray()},squad,service.Staff(team),youthTeam:new SqliteClubSquadStore(path).Teams().Single(t=>t.Id==team).Category=="Juvenil");
        if(plan.RequiresReview||day!=service.CareerDate()||DateOnly.FromDateTime(clock.CareerTime)!=day||observations.Any(e=>!clock.Verified(team,e)||e.Date!=day||plan.Sessions.SingleOrDefault(s=>s.Id==e.Session) is not {} session||session.Ends>clock.CareerTime))throw new InvalidOperationException("Falta tiempo/evidencia real o la planificación requiere revisión.");
        // Punto de integración protegido: no se activa el ejecutor desde la interfaz.
        var restrictions=new SqliteTrainingAvailabilityStore(path).Restrictions();
        foreach(var session in plan.Sessions.Where(s=>s.Date==day&&!completed.Contains(s.Id)))TrainingAvailability.Validate(session,restrictions);
        var states=new List<DevelopmentState>();foreach(var p in squad.Where(p=>observations.Any(e=>e.Attended.Contains(p.Id)))){if(!physical.TryGetValue(p.Id,out var state))throw new InvalidOperationException("No existe observación física de ese futbolista.");states.Add(DevelopmentEngine.Process(p,State(p.Id),day,plan.Sessions,observations,state));}
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();SqliteSportsStore.Ensure(c,t);foreach(var observation in observations){using var record=WorldDatabase.Command(c,t,"INSERT INTO training_attendance VALUES($id,$team,$date,$json)",("$id",observation.Session.ToString("D")),("$team",team.ToString("D")),("$date",day.ToString("yyyy-MM-dd")),("$json",CareerJson.Write(new {Session=plan.Sessions.Single(s=>s.Id==observation.Session),Observation=observation,Model=DevelopmentEngine.Version})));record.ExecuteNonQuery();}foreach(var state in states){using var q=WorldDatabase.Command(c,t,"INSERT INTO training_development VALUES($player,$team,$json) ON CONFLICT(player_id) DO UPDATE SET team_id=excluded.team_id,data_json=excluded.data_json;",("$player",state.Player.ToString()),("$team",team.ToString("D")),("$json",CareerJson.Write(state)));q.ExecuteNonQuery();}t.Commit();
    }
}
