using Furvo.Core.Domain;
using Furvo.Core.Development;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Disponibilidad administrativa aislada por archivo de carrera. Las lecturas nunca crean tablas.</summary>
public sealed class SqliteTrainingAvailabilityStore(string path)
{
    private static bool HasSchema(SqliteConnection c,SqliteTransaction t)
    {
        using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='training_availability_schema';");
        if(Convert.ToInt32(q.ExecuteScalar())==0)return false;
        q.CommandText="SELECT version FROM training_availability_schema WHERE singleton=1";
        if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de disponibilidad desconocida.");return true;
    }
    internal static IReadOnlyList<TrainingRestriction> Read(SqliteConnection c,SqliteTransaction t)
    {
        _=SqliteLineupStore.Owner(c,t);if(!HasSchema(c,t))return Array.Empty<TrainingRestriction>();
        using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM training_availability ORDER BY starts_on,id;");using var r=q.ExecuteReader();var result=new List<TrainingRestriction>();
        while(r.Read()){var entry=CareerJson.Read<TrainingRestriction>(r.GetString(0));entry.Validate();result.Add(entry);}return result;
    }
    public IReadOnlyList<TrainingRestriction> Restrictions()
    {using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);return Read(c,t);}
    public TrainingRestriction Save(TrainingRestriction restriction)
    {
        restriction.Validate();if(!new SqliteClubSquadStore(path).All(false).ContainsKey(restriction.Player))throw new InvalidOperationException("Futbolista ajeno al club dirigido.");
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();_=SqliteLineupStore.Owner(c,t);
        if(!HasSchema(c,t))CareerJson.Migrate(c,t,"TrainingAvailability001_Initial.sql");
        var old=Read(c,t).SingleOrDefault(r=>r.Id==restriction.Id);
        if(old?.Player is {} player&&player!=restriction.Player)throw new InvalidOperationException("No se cambia la identidad del futbolista de una restricción.");
        if((old?.Revision??0)!=restriction.Revision)throw new InvalidOperationException("La restricción cambió. Vuelve a abrirla antes de guardar.");
        var next=restriction with{Revision=restriction.Revision+1};
        using var q=WorldDatabase.Command(c,t,"INSERT INTO training_availability VALUES($id,$player,$start,$end,$revision,$json) ON CONFLICT(id) DO UPDATE SET starts_on=excluded.starts_on,ends_on=excluded.ends_on,revision=excluded.revision,data_json=excluded.data_json;",
            ("$id",next.Id.ToString("D")),("$player",next.Player.ToString()),("$start",next.Start.ToString("yyyy-MM-dd")),("$end",next.End is {} end?(object)end.ToString("yyyy-MM-dd"):DBNull.Value),("$revision",next.Revision),("$json",CareerJson.Write(next)));
        q.ExecuteNonQuery();t.Commit();return next;
    }
    public TrainingRestriction Finish(Guid id,DateOnly inclusiveEnd,int revision)
    {var old=Restrictions().SingleOrDefault(r=>r.Id==id)??throw new InvalidOperationException("Restricción inexistente.");return Save(old with{End=inclusiveEnd,Revision=revision});}
    public IReadOnlyList<TrainingAvailabilityConflict> Conflicts(Guid team)
    {var sports=new SqliteSportsStore(path);return TrainingAvailability.Conflicts(sports.Plan(team).Sessions,Restrictions(),sports.CareerDate(),new SqliteTrainingExecutionStore(path).CompletedSessions(team));}
}
