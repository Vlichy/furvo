using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Lectura sin migraciones automáticas y edición transaccional con revisión optimista. Nunca escribe mundos iniciales.</summary>
public sealed class SqliteLineupStore(string databasePath)
{
    private readonly string _path=WorldDatabase.PathFor(databasePath);
    private static bool HasSchema(SqliteConnection c,SqliteTransaction t)
    {
        using var cmd=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='lineup_schema_version';");
        if(Convert.ToInt32(cmd.ExecuteScalar())==0)return false;
        cmd.CommandText="SELECT version FROM lineup_schema_version WHERE singleton=1;";
        if(Convert.ToInt32(cmd.ExecuteScalar())!=1)throw new NotSupportedException("Versión de alineación no compatible.");return true;
    }
    private static (SaveId Save,ClubId? Club,DateOnly Date) Owner(SqliteConnection c,SqliteTransaction t)
    {
        var info=WorldDatabase.Info(c,t);if(info.Kind!=WorldFileKind.Save)throw new InvalidOperationException("Las alineaciones pertenecen a carreras, no al mundo inicial.");
        using var cmd=WorldDatabase.Command(c,t,"SELECT employment,club_id,current_date FROM career_state WHERE singleton=1;");using var r=cmd.ExecuteReader();
        if(!r.Read())throw new InvalidDataException("Falta carrera.");return(info.SaveId!.Value,r.GetInt32(0)==2?new ClubId(Guid.Parse(r.GetString(1))):null,DateOnly.ParseExact(r.GetString(2),"yyyy-MM-dd"));
    }
    private static CareerLineup? Read(SqliteConnection c,SqliteTransaction t)
    {
        var owner=Owner(c,t);if(!HasSchema(c,t)||owner.Club is null)return null;
        using var cmd=WorldDatabase.Command(c,t,"SELECT formation_json,bench_limit,revision,(SELECT seed FROM career_lineup_seed WHERE singleton=1) FROM career_lineups WHERE club_id=$club;",("$club",owner.Club.Value.ToString()));
        FormationDefinition formation;int bench,revision;string seed;
        using(var r=cmd.ExecuteReader()){if(!r.Read())return null;formation=CareerJson.Read<FormationDefinition>(r.GetString(0));bench=r.GetInt32(1);revision=r.GetInt32(2);seed=r.GetString(3);}
        using var rows=WorldDatabase.Command(c,t,"SELECT player_id,assignment,slot_id FROM career_lineup_members WHERE club_id=$club ORDER BY player_id;",("$club",owner.Club.Value.ToString()));
        using var reader=rows.ExecuteReader();var members=new List<LineupMember>();while(reader.Read())members.Add(new(new PlayerId(Guid.Parse(reader.GetString(0))),(LineupRole)reader.GetInt32(1),reader.IsDBNull(2)?null:reader.GetString(2)));
        return new(owner.Club.Value,seed,formation,bench,revision,members.AsReadOnly());
    }
    public CareerLineup? Load()
    {
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var result=Read(c,t);
        if(result is not null)LineupSelection.Validate(result,Players());return result;
    }
    public IReadOnlyList<LineupPlayer> Players()
    {
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var owner=Owner(c,t);
        if(owner.Club is null)throw new InvalidOperationException("El entrenador no tiene club.");
        var q=new WorldQueries(new SqliteWorldStore(_path).Load(),owner.Date);
        return Array.AsReadOnly(q.GetSquad(owner.Club.Value).Select(p=>LineupPlayer.From(q.GetPlayer(owner.Club.Value,p.Id))).ToArray());
    }
    internal static void Initialize(SqliteConnection c,SqliteTransaction t,WorldData world,SaveId save,ClubId? club,DateOnly date)
    {
        if(!HasSchema(c,t))CareerJson.Migrate(c,t,"Lineups001_Initial.sql");
        using(var seed=WorldDatabase.Command(c,t,"INSERT OR IGNORE INTO career_lineup_seed VALUES(1,$seed);",("$seed",LineupSelection.SeedFor(save.Value))))seed.ExecuteNonQuery();
        if(club is null||Read(c,t) is not null)return;
        using var readSeed=WorldDatabase.Command(c,t,"SELECT seed FROM career_lineup_seed WHERE singleton=1;");var value=(string)readSeed.ExecuteScalar()!;
        var q=new WorldQueries(world,date);var players=q.GetSquad(club.Value).Select(p=>LineupPlayer.From(q.GetPlayer(club.Value,p.Id))).ToArray();
        var lineup=LineupSelection.Generate(club.Value,value,FormationCatalog.Initial(club.Value),players,FormationCatalog.BenchLimit);
        using(var header=WorldDatabase.Command(c,t,"INSERT INTO career_lineups VALUES($club,$formation,$bench,0,$policy);",("$club",club.Value.ToString()),("$formation",CareerJson.Write(lineup.Formation)),("$bench",lineup.BenchLimit),("$policy",LineupSelection.PolicyVersion)))header.ExecuteNonQuery();
        WriteMembers(c,t,lineup);
    }
    /// <summary>Única vía de ampliación de carreras sin extensión: acción explícita, nunca Load ni apertura de pantalla.</summary>
    public CareerLineup InitializeMissing()
    {
        var players=Players();var world=new SqliteWorldStore(_path).Load();
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var owner=Owner(c,t);
        if(owner.Club is null)throw new InvalidOperationException("Sin club.");
        Initialize(c,t,world,owner.Save,owner.Club,owner.Date);var state=Read(c,t)!;LineupSelection.Validate(state,players);t.Commit();return state;
    }
    public CareerLineup Save(CareerLineup proposed)
    {
        var players=Players();LineupSelection.Validate(proposed,players);
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var owner=Owner(c,t);var previous=Read(c,t)??throw new InvalidOperationException("Inicializa la alineación primero.");
        if(owner.Club!=proposed.ClubId||previous.Seed!=proposed.Seed||previous.BenchLimit!=proposed.BenchLimit)throw new InvalidOperationException("Identidad o semilla incompatible.");
        using var update=WorldDatabase.Command(c,t,"UPDATE career_lineups SET formation_json=$formation,revision=revision+1 WHERE club_id=$club AND revision=$revision;",("$formation",CareerJson.Write(proposed.Formation)),("$club",proposed.ClubId.ToString()),("$revision",proposed.Revision));
        if(update.ExecuteNonQuery()!=1)throw new InvalidOperationException("La alineación cambió en otra operación; vuelve a abrirla.");
        using(var delete=WorldDatabase.Command(c,t,"DELETE FROM career_lineup_members WHERE club_id=$club;",("$club",proposed.ClubId.ToString())))delete.ExecuteNonQuery();
        WriteMembers(c,t,proposed);t.Commit();return proposed with{Revision=proposed.Revision+1};
    }
    private static void WriteMembers(SqliteConnection c,SqliteTransaction t,CareerLineup state)
    {
        foreach(var member in state.Members){using var row=WorldDatabase.Command(c,t,"INSERT INTO career_lineup_members VALUES($club,$player,$role,$slot);",("$club",state.ClubId.ToString()),("$player",member.PlayerId.ToString()),("$role",(int)member.Role),("$slot",member.SlotId));row.ExecuteNonQuery();}
    }
}
