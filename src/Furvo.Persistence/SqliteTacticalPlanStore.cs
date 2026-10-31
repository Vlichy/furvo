using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
public interface ILineupEditorStore
{
    CareerLineup? Load();
    IReadOnlyList<LineupPlayer> Players();
    CareerLineup Save(CareerLineup proposed);
    CareerLineup InitializeMissing();
}
/// <summary>Fuente canónica de los planes. Lectura v0 proyectada, migración y retirada de la composición antigua solo al guardar.</summary>
public interface ITacticalPlanStore
{
    TacticalPlanBook? Load();
    TacticalPlanBook Save(TacticalPlanBook proposed,TacticalSettings? common=null);
    TacticalSettings Settings(TacticalPlanBook book,TacticalPlanKind kind);
    TacticalSettings SaveSettings(TacticalPlanKind kind,TacticalSettings proposed);
}
public sealed class SqliteTacticalPlanStore(string databasePath):ITacticalPlanStore
{
    private readonly string _path=WorldDatabase.PathFor(databasePath);
    internal static bool HasSchema(SqliteConnection c,SqliteTransaction t)
    {
        using var cmd=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='tactical_plans_schema' AND type='table';");
        if(Convert.ToInt32(cmd.ExecuteScalar())==0)return false;
        cmd.CommandText="SELECT version FROM tactical_plans_schema WHERE singleton=1;";
        if(Convert.ToInt32(cmd.ExecuteScalar())!=1)throw new NotSupportedException("Versión de planes no compatible.");return true;
    }
    public bool IsPersisted{get{using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);return HasSchema(c,t);}}
    internal static TacticalPlanBook? ReadPersisted(SqliteConnection c,SqliteTransaction t)
    {
        if(!HasSchema(c,t))return null;var owner=SqliteLineupStore.Owner(c,t);if(owner.Club is null)return null;
        using var header=WorldDatabase.Command(c,t,"SELECT revision FROM career_plan_books WHERE club_id=$club;",("$club",owner.Club.Value.ToString()));
        var raw=header.ExecuteScalar();if(raw is null)throw new InvalidDataException("Falta cabecera de planes.");
        var plans=new List<TacticalPlan>();
        using var exists=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='career_slot_behaviours' AND type='table';");var hasBehaviour=Convert.ToInt32(exists.ExecuteScalar())==1;
        using var cmd=WorldDatabase.Command(c,t,"SELECT id,kind,seed,bench_limit,mentality,instructions_json FROM career_tactical_plans WHERE club_id=$club ORDER BY kind;",("$club",owner.Club.Value.ToString()));
        var rows=new List<(Guid Id,TacticalPlanKind Kind,string Seed,int Bench,TeamMentality? Mentality,Dictionary<string,string?> Instructions)>();
        using(var r=cmd.ExecuteReader())while(r.Read())rows.Add((Guid.Parse(r.GetString(0)),(TacticalPlanKind)r.GetInt32(1),r.GetString(2),r.GetInt32(3),r.IsDBNull(4)?null:(TeamMentality)r.GetInt32(4),CareerJson.Read<Dictionary<string,string?>>(r.GetString(5))));
        foreach(var row in rows)
        {
            var members=new List<LineupMember>();using(var m=WorldDatabase.Command(c,t,"SELECT player_id,assignment,slot_id FROM career_plan_members WHERE plan_id=$id ORDER BY player_id;",("$id",row.Id.ToString("D"))))
            using(var r=m.ExecuteReader())while(r.Read())members.Add(new(new PlayerId(Guid.Parse(r.GetString(0))),(LineupRole)r.GetInt32(1),r.IsDBNull(2)?null:r.GetString(2)));
            var phases=new Dictionary<PossessionPhase,TacticalDisposition>();using(var p=WorldDatabase.Command(c,t,"SELECT phase,reference_json,positions_json,customized FROM career_plan_dispositions WHERE plan_id=$id ORDER BY phase;",("$id",row.Id.ToString("D"))))
            using(var r=p.ExecuteReader())while(r.Read())phases.Add((PossessionPhase)r.GetInt32(0),new(CareerJson.Read<FormationDefinition>(r.GetString(1)),CareerJson.Read<FormationSlot[]>(r.GetString(2)),r.GetInt32(3)==1));
            if(hasBehaviour){using var b=WorldDatabase.Command(c,t,"SELECT phase,data_json FROM career_slot_behaviours WHERE plan_id=$id;",("$id",row.Id.ToString("D")));using var br=b.ExecuteReader();while(br.Read()){var phase=(PossessionPhase)br.GetInt32(0);var values=CareerJson.Read<Dictionary<string,SlotBehaviour>>(br.GetString(1));phases[phase]=phases[phase] with{Behaviours=values.Count==0?null:values};}}
            if(phases.Count!=2)throw new InvalidDataException("Faltan disposiciones del plan.");
            plans.Add(new(row.Id,row.Kind,new(row.Seed,row.Bench,members.AsReadOnly()),phases[PossessionPhase.WithBall],phases[PossessionPhase.WithoutBall],row.Mentality,TacticalCatalog.UpgradeInstructions(row.Instructions)));
        }
        return new(owner.Club.Value,Convert.ToInt32(raw),plans.AsReadOnly());
    }
    private static TacticalPlanBook? Read(SqliteConnection c,SqliteTransaction t)
    {
        if(HasSchema(c,t))return ReadPersisted(c,t);
        var owner=SqliteLineupStore.Owner(c,t);if(owner.Club is null)return null;
        var lineup=SqliteLineupStore.ReadLegacy(c,t);if(lineup is null)return null;
        var settings=SqliteTacticsStore.ReadRaw(c,t)??TacticalSettings.Undefined(owner.Club.Value);
        return TacticalPlanRules.FromLegacy(owner.Save.Value,lineup,settings) with{Revision=checked(lineup.Revision+settings.Revision)};
    }
    public TacticalPlanBook? Load()
    {
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var book=Read(c,t);
        if(book is not null)TacticalPlanRules.Validate(book,new SqliteLineupStore(_path).Players());return book;
    }
    public TacticalSettings Settings(TacticalPlanBook book,TacticalPlanKind kind)
    {
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);
        var common=SqliteTacticsStore.ReadRaw(c,t)??TacticalSettings.Undefined(book.ClubId);var p=book.Get(kind);
        return common with{Revision=book.Revision,Mentality=p.Mentality,Instructions=p.Instructions};
    }
    public TacticalPlanBook Save(TacticalPlanBook proposed,TacticalSettings? common=null)
    {
        var players=new SqliteLineupStore(_path).Players();TacticalPlanRules.Validate(proposed,players);
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var current=Read(c,t)??throw new InvalidOperationException("Inicializa primero el once.");
        if(current.ClubId!=proposed.ClubId||current.Revision!=proposed.Revision)throw new InvalidOperationException("Los planes cambiaron en otra operación; vuelve a abrirlos.");
        if(proposed.Plans.Any(p=>current.Get(p.Kind).Id!=p.Id||current.Get(p.Kind).Roster.BenchLimit!=p.Roster.BenchLimit))throw new InvalidDataException("Identidad de plan o límite de suplentes incompatible.");
        var raw=SqliteTacticsStore.ReadRaw(c,t)??TacticalSettings.Undefined(proposed.ClubId);
        var shared=(common??raw) with{ClubId=proposed.ClubId,Revision=raw.Revision,Mentality=null,Instructions=TacticalSettings.Undefined(proposed.ClubId).Instructions};
        TacticalCatalog.Validate(shared,players.Select(p=>p.Id).ToHashSet());
        var next=WriteAtomic(c,t,proposed,shared);t.Commit();return next;
    }
    internal static TacticalPlanBook WriteAtomic(SqliteConnection c,SqliteTransaction t,TacticalPlanBook proposed,TacticalSettings shared)
    {
        if(!HasSchema(c,t))CareerJson.Migrate(c,t,"TacticalPlans001_Initial.sql");
        SqliteTacticsStore.WriteRaw(c,t,shared);
        if(SqliteSportsStore.HasSchema(c,t))CareerJson.Migrate(c,t,"SportsTactics002_Assignment.sql");
        CareerJson.Migrate(c,t,"TacticalBehaviour001_Initial.sql");
        // Se retira la alineación anterior: los adaptadores la consultan ahora desde Inicial/Sin balón.
        using(var clear=WorldDatabase.Command(c,t,"DELETE FROM career_lineup_members; DELETE FROM career_lineups;"))clear.ExecuteNonQuery();
        var next=proposed with{Revision=proposed.Revision+1};
        using(var h=WorldDatabase.Command(c,t,"INSERT INTO career_plan_books VALUES($club,$revision) ON CONFLICT(club_id) DO UPDATE SET revision=excluded.revision;",("$club",next.ClubId.ToString()),("$revision",next.Revision)))h.ExecuteNonQuery();
        using(var clear=WorldDatabase.Command(c,t,"DELETE FROM career_slot_behaviours; DELETE FROM career_plan_members; DELETE FROM career_plan_dispositions; DELETE FROM career_tactical_plans;"))clear.ExecuteNonQuery();
        foreach(var plan in next.Plans)
        {
            using(var p=WorldDatabase.Command(c,t,"INSERT INTO career_tactical_plans VALUES($id,$club,$kind,$seed,$bench,$mental,$instructions);",("$id",plan.Id.ToString("D")),("$club",next.ClubId.ToString()),("$kind",(int)plan.Kind),("$seed",plan.Roster.Seed),("$bench",plan.Roster.BenchLimit),("$mental",plan.Mentality is {} mentality?(int)mentality:null),("$instructions",CareerJson.Write(plan.Instructions))))p.ExecuteNonQuery();
            foreach(var member in plan.Roster.Members){using var m=WorldDatabase.Command(c,t,"INSERT INTO career_plan_members VALUES($plan,$player,$role,$slot);",("$plan",plan.Id.ToString("D")),("$player",member.PlayerId.ToString()),("$role",(int)member.Role),("$slot",member.SlotId));m.ExecuteNonQuery();}
            foreach(var phase in Enum.GetValues<PossessionPhase>()){var d=plan.Disposition(phase);using var p=WorldDatabase.Command(c,t,"INSERT INTO career_plan_dispositions VALUES($plan,$phase,$reference,$positions,$custom);",("$plan",plan.Id.ToString("D")),("$phase",(int)phase),("$reference",CareerJson.Write(d.Reference)),("$positions",CareerJson.Write(d.Positions)),("$custom",d.IsCustom?1:0));p.ExecuteNonQuery();using var behaviour=WorldDatabase.Command(c,t,"INSERT INTO career_slot_behaviours VALUES($plan,$phase,$json);",("$plan",plan.Id.ToString("D")),("$phase",(int)phase),("$json",CareerJson.Write(d.Behaviours??new Dictionary<string,SlotBehaviour>())));behaviour.ExecuteNonQuery();}
        }
        return next;
    }
    public TacticalSettings SaveSettings(TacticalPlanKind kind,TacticalSettings proposed)
    {
        var book=Load()??throw new InvalidOperationException("Sin planes.");if(book.Revision!=proposed.Revision)throw new InvalidOperationException("La táctica cambió. Vuelve a abrirla.");
        var plan=book.Get(kind) with{Mentality=proposed.Mentality,Instructions=proposed.Instructions.ToDictionary()};
        return Settings(Save(book.Replace(plan),proposed),kind);
    }
}
public sealed class SqlitePlanLineupStore(string path,TacticalPlanKind kind,PossessionPhase phase):ILineupEditorStore
{
    public CareerLineup? Load(){var b=new SqliteTacticalPlanStore(path).Load();return b?.Get(kind).View(b.ClubId,b.Revision,phase);}
    public IReadOnlyList<LineupPlayer> Players()=>new SqliteLineupStore(path).Players();
    public CareerLineup InitializeMissing(){new SqliteLineupStore(path).InitializeMissing();return Load()!;}
    public CareerLineup Save(CareerLineup proposed)
    {
        var store=new SqliteTacticalPlanStore(path);var book=store.Load()??throw new InvalidOperationException("Sin once.");
        if(book.Revision!=proposed.Revision||book.ClubId!=proposed.ClubId)throw new InvalidOperationException("La alineación cambió. Vuelve a abrirla.");
        var plan=book.Get(kind);if(plan.Roster.Seed!=proposed.Seed||plan.Roster.BenchLimit!=proposed.BenchLimit)throw new InvalidDataException("Identidad incompatible.");
        plan=TacticalPlanRules.ApplyLineup(plan,phase,proposed);
        var saved=store.Save(book.Replace(plan));return saved.Get(kind).View(saved.ClubId,saved.Revision,phase);
    }
}
