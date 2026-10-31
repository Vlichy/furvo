using Furvo.Core.Domain;
using Furvo.Core.Tactics;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Extensión independiente y transaccional. Abrir o leer no migra ni inicializa valores tácticos.</summary>
public sealed class SqliteTacticsStore(string databasePath)
{
    private readonly string _path=WorldDatabase.PathFor(databasePath);
    private static int SchemaVersion(SqliteConnection c,SqliteTransaction t)
    {
        using var cmd=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='tactics_schema_version';");
        if(Convert.ToInt32(cmd.ExecuteScalar())==0)return 0;
        cmd.CommandText="SELECT version FROM tactics_schema_version WHERE singleton=1;";
        var version=Convert.ToInt32(cmd.ExecuteScalar());if(version is <1 or >4)throw new NotSupportedException("Versión táctica no compatible.");
        return version;
    }
    internal static TacticalSettings? ReadRaw(SqliteConnection c,SqliteTransaction t)
    {
        var owner=SqliteLineupStore.Owner(c,t);var version=SchemaVersion(c,t);if(owner.Club is null||version==0)return null;
        using var cmd=WorldDatabase.Command(c,t,"SELECT revision,mentality,instructions_json,data_version FROM career_tactics WHERE club_id=$club;",("$club",owner.Club.Value.ToString()));
        int revision;TeamMentality? mentality;Dictionary<string,string?> instructions;
        using(var r=cmd.ExecuteReader()){
            if(!r.Read())return null;if(r.GetInt32(3)!=TacticalCatalog.DataVersion)throw new NotSupportedException("Datos tácticos no compatibles.");
            revision=r.GetInt32(0);mentality=r.IsDBNull(1)?null:(TeamMentality)r.GetInt32(1);instructions=TacticalCatalog.UpgradeInstructions(CareerJson.Read<Dictionary<string,string?>>(r.GetString(2)));
        }
        using var rows=WorldDatabase.Command(c,t,"SELECT kind,routine,taker_id"+(version>=2?",details_json":"")+" FROM career_set_pieces WHERE club_id=$club ORDER BY kind;",("$club",owner.Club.Value.ToString()));
        var pieces=new Dictionary<SetPieceKind,SetPiecePlan>();
        using(var reader=rows.ExecuteReader())
            while(reader.Read())pieces.Add((SetPieceKind)reader.GetInt32(0),new(reader.IsDBNull(1)?null:reader.GetString(1),reader.IsDBNull(2)?null:new PlayerId(Guid.Parse(reader.GetString(2))),version>=2&&!reader.IsDBNull(3)?CareerJson.Read<SetPieceDetails>(reader.GetString(3)):null));
        foreach(var kind in pieces.Keys.ToArray())pieces[kind]=TacticalCatalog.UpgradePlan(pieces[kind]);
        var locations=new Dictionary<string,SetPiecePlan>();
        if(version>=3){using var q=WorldDatabase.Command(c,t,"SELECT kind,location,plan_json FROM career_free_kicks WHERE club_id=$club;",("$club",owner.Club.Value.ToString()));using var r=q.ExecuteReader();while(r.Read())locations.Add(TacticalSettings.FreeKickKey((SetPieceKind)r.GetInt32(0),(FreeKickLocation)r.GetInt32(1)),TacticalCatalog.UpgradePlan(CareerJson.Read<SetPiecePlan>(r.GetString(2))));}
        var order=new PlayerId?[5];
        if(version>=2){using var penalty=WorldDatabase.Command(c,t,"SELECT ordinal,player_id FROM career_penalty_order WHERE club_id=$club ORDER BY ordinal;",("$club",owner.Club.Value.ToString()));using var r=penalty.ExecuteReader();while(r.Read())order[r.GetInt32(0)-1]=r.IsDBNull(1)?null:new PlayerId(Guid.Parse(r.GetString(1)));}
        TeamPersonnel? personnel=null;
        if(version>=4){using var p=WorldDatabase.Command(c,t,"SELECT data_json FROM career_tactical_personnel WHERE club_id=$club;",("$club",owner.Club.Value.ToString()));if(p.ExecuteScalar() is string json)personnel=CareerJson.Read<TeamPersonnel>(json);}
        return new(owner.Club.Value,revision,mentality,instructions,pieces,Array.AsReadOnly(order),locations,personnel);
    }
    public TacticalSettings? Load()
    {
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var result=ReadRaw(c,t);
        if(SqliteTacticalPlanStore.ReadPersisted(c,t) is {} book){var plan=book.Get(TacticalPlanKind.Initial);result=(result??TacticalSettings.Undefined(book.ClubId)) with{Revision=book.Revision,Mentality=plan.Mentality,Instructions=plan.Instructions};}
        if(result is not null)TacticalCatalog.Validate(result,new SqliteLineupStore(_path).Players().Select(p=>p.Id).ToHashSet());return result;
    }
    public TacticalSettings Save(TacticalSettings proposed)
    {
        if(new SqliteTacticalPlanStore(_path).IsPersisted)return new SqliteTacticalPlanStore(_path).SaveSettings(TacticalPlanKind.Initial,proposed);
        var squad=new SqliteLineupStore(_path).Players().Select(p=>p.Id).ToHashSet();TacticalCatalog.Validate(proposed,squad);
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var owner=SqliteLineupStore.Owner(c,t);
        if(owner.Club!=proposed.ClubId)throw new InvalidOperationException("No dirige este club.");
        var next=WriteRaw(c,t,proposed);t.Commit();return next;
    }
    internal static void EnsureSchema(SqliteConnection c,SqliteTransaction t)
    {
        if(SchemaVersion(c,t)==0)CareerJson.Migrate(c,t,"Tactics001_Initial.sql");
        if(SchemaVersion(c,t)==1)CareerJson.Migrate(c,t,"Tactics002_SetPieceAssignments.sql");
        if(SchemaVersion(c,t)==2)CareerJson.Migrate(c,t,"Tactics003_FreeKickLocations.sql");
        if(SchemaVersion(c,t)==3)CareerJson.Migrate(c,t,"Tactics004_Personnel.sql");
    }
    internal static TacticalSettings WriteRaw(SqliteConnection c,SqliteTransaction t,TacticalSettings proposed)
    {
        EnsureSchema(c,t);
        var old=ReadRaw(c,t);
        if((old?.Revision??0)!=proposed.Revision)throw new InvalidOperationException("La táctica cambió en otra operación. Vuelve a abrirla.");
        var next=proposed with{Revision=proposed.Revision+1};
        using(var header=WorldDatabase.Command(c,t,"INSERT INTO career_tactics VALUES($club,$revision,$mentality,$json,1) ON CONFLICT(club_id) DO UPDATE SET revision=excluded.revision,mentality=excluded.mentality,instructions_json=excluded.instructions_json;",
            ("$club",next.ClubId.ToString()),("$revision",next.Revision),("$mentality",next.Mentality is {} m?(int)m:null),("$json",CareerJson.Write(next.Instructions))))header.ExecuteNonQuery();
        using(var delete=WorldDatabase.Command(c,t,"DELETE FROM career_set_pieces WHERE club_id=$club;",("$club",next.ClubId.ToString())))delete.ExecuteNonQuery();
        foreach(var (kind,plan) in next.SetPieces){using var insert=WorldDatabase.Command(c,t,"INSERT INTO career_set_pieces(club_id,kind,routine,taker_id,details_json) VALUES($club,$kind,$routine,$taker,$details);",
            ("$club",next.ClubId.ToString()),("$kind",(int)kind),("$routine",plan.Routine),("$taker",plan.Taker?.ToString()),("$details",plan.Details is null?null:CareerJson.Write(plan.Details)));insert.ExecuteNonQuery();}
        using(var delete=WorldDatabase.Command(c,t,"DELETE FROM career_penalty_order WHERE club_id=$club;",("$club",next.ClubId.ToString())))delete.ExecuteNonQuery();
        for(var i=0;i<5;i++){using var row=WorldDatabase.Command(c,t,"INSERT INTO career_penalty_order VALUES($club,$ordinal,$player);",("$club",next.ClubId.ToString()),("$ordinal",i+1),("$player",next.ShootoutOrder[i]?.ToString()));row.ExecuteNonQuery();}
        using(var clear=WorldDatabase.Command(c,t,"DELETE FROM career_free_kicks WHERE club_id=$club;",("$club",next.ClubId.ToString())))clear.ExecuteNonQuery();
        foreach(var (key,plan) in next.FreeKicks??new Dictionary<string,SetPiecePlan>()){var parts=key.Split(':');using var row=WorldDatabase.Command(c,t,"INSERT INTO career_free_kicks VALUES($club,$kind,$location,$json);",("$club",next.ClubId.ToString()),("$kind",int.Parse(parts[0])),("$location",int.Parse(parts[1])),("$json",CareerJson.Write(plan)));row.ExecuteNonQuery();}
        using(var personnel=WorldDatabase.Command(c,t,"INSERT INTO career_tactical_personnel VALUES($club,$json) ON CONFLICT(club_id) DO UPDATE SET data_json=excluded.data_json;",("$club",next.ClubId.ToString()),("$json",CareerJson.Write(next.Personnel??TeamPersonnel.Empty()))))personnel.ExecuteNonQuery();
        return next;
    }
}
