using Furvo.Core.Academy;
using Furvo.Core.Development;
using Furvo.Core.Domain;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Expediente administrativo por carrera. Reutiliza las instantáneas de Análisis, nunca las reconstruye al consultar.</summary>
public sealed class SqliteAcademyFollowupStore(string path)
{
    private static bool HasSchema(SqliteConnection c,SqliteTransaction t){using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='academy_followup_schema'");if(Convert.ToInt32(q.ExecuteScalar())==0)return false;q.CommandText="SELECT version FROM academy_followup_schema WHERE singleton=1";if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de seguimiento desconocida.");return true;}
    private void Player(PlayerId player){if(!new SqliteClubSquadStore(path).All(false).ContainsKey(player))throw new InvalidOperationException("Futbolista ajeno al club dirigido.");}
    private static AcademyObservation Read(SqliteConnection c,SqliteTransaction t,Guid id){if(!HasSchema(c,t))throw new KeyNotFoundException("Observación inexistente.");using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM academy_observations WHERE id=$id",("$id",id.ToString("D")));return q.ExecuteScalar() is string json?CareerJson.Read<AcademyObservation>(json):throw new KeyNotFoundException("Observación inexistente.");}
    private Guid Principal(SqliteConnection c,SqliteTransaction t){_=SqliteLineupStore.Owner(c,t);using var q=WorldDatabase.Command(c,t,"SELECT coach_id FROM career_state WHERE singleton=1");return Guid.Parse((string)q.ExecuteScalar()!);}
    private void Authorize(Guid team,Guid actor){_=new SqliteAcademyStore(path).Get(team);if(!new SqliteAnalysisReportService(path).Staff(team,AnalysisReportKind.Individual).Any(a=>a.Id==actor))throw new UnauthorizedAccessException("El responsable no está autorizado en este equipo.");}
    public IReadOnlyList<AcademyObservation> Observations(PlayerId player)
    {Player(player);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(!HasSchema(c,t))return Array.Empty<AcademyObservation>();using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM academy_observations WHERE player_id=$player ORDER BY registered_on,id",("$player",player.ToString()));using var r=q.ExecuteReader();var result=new List<AcademyObservation>();while(r.Read()){var note=CareerJson.Read<AcademyObservation>(r.GetString(0));note.Validate();result.Add(note);}return result;}
    public AcademyObservation Create(Guid id,Guid team,PlayerId player,string text,Guid actor)
    {
        Player(player);Authorize(team,actor);if(!new SqliteClubSquadStore(path).Candidates(team).Any(p=>p.Id==player))throw new InvalidDataException("El futbolista no pertenece a ese equipo inferior.");
        var author=new SqliteAnalysisReportService(path).Staff(team,AnalysisReportKind.Individual).Single(a=>a.Id==actor);using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var value=new AcademyObservation(id,player,team,SqliteLineupStore.Owner(c,t).Date,actor,author.Name,text.Trim());value.Validate();
        if(!HasSchema(c,t))CareerJson.Migrate(c,t,"AcademyFollowup001_Initial.sql");using var q=WorldDatabase.Command(c,t,"INSERT INTO academy_observations VALUES($id,$player,$team,$date,1,$json)",("$id",id.ToString("D")),("$player",player.ToString()),("$team",team.ToString("D")),("$date",value.RegisteredOn.ToString("yyyy-MM-dd")),("$json",CareerJson.Write(value)));q.ExecuteNonQuery();t.Commit();return value;
    }
    public AcademyObservation Edit(Guid id,string text,Guid actor,int revision)
    {
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);Player(old.Player);Authorize(old.Team,actor);if(!old.CanEdit(actor,Principal(c,t)))throw new UnauthorizedAccessException("Solo el autor o el entrenador principal pueden editar esta observación.");if(old.Revision!=revision)throw new InvalidOperationException("La observación cambió. Vuelve a abrirla.");
        var next=old with{Text=text.Trim(),ModifiedOn=SqliteLineupStore.Owner(c,t).Date,Revision=old.Revision+1};next.Validate();using var q=WorldDatabase.Command(c,t,"UPDATE academy_observations SET revision=$revision,data_json=$json WHERE id=$id AND revision=$old",("$revision",next.Revision),("$json",CareerJson.Write(next)),("$id",id.ToString("D")),("$old",revision));if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("La observación cambió.");t.Commit();return next;
    }
    public void Delete(Guid id,Guid actor,int revision)
    {
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);Player(old.Player);Authorize(old.Team,actor);if(!old.CanEdit(actor,Principal(c,t)))throw new UnauthorizedAccessException("Solo el autor o el entrenador principal pueden eliminar esta observación.");using var q=WorldDatabase.Command(c,t,"DELETE FROM academy_observations WHERE id=$id AND revision=$revision",("$id",id.ToString("D")),("$revision",revision));if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("La observación cambió. Vuelve a abrirla.");t.Commit();
    }
    public IReadOnlyList<AnalysisReport> Reports(PlayerId player)
    {Player(player);var sports=new SqliteSportsStore(path);return new SqliteClubSquadStore(path).Teams().SelectMany(team=>sports.Reports(team.Id)).Where(r=>AcademyFollowup.ContainsPlayer(r,player)).OrderByDescending(r=>r.Date).ThenBy(r=>r.Id).ToArray();}
}
