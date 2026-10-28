using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;

/// <summary>Estado económico por club y temporada. Las consultas son de solo lectura, incluso para carreras históricas.</summary>
public static class SqliteEconomyStore
{
    internal static long Cents(decimal amount)=>checked((long)(amount*100));
    public static IReadOnlyList<ClubFinancialResources> ReadResources(string databasePath)
    {
        using var c=WorldDatabase.Open(databasePath);using var t=c.BeginTransaction(deferred:true);
        _=WorldDatabase.Info(c,t);return ReadResources(c,t);
    }
    internal static IReadOnlyList<ClubFinancialResources> ReadResources(SqliteConnection c,SqliteTransaction t)
    {
        using var exists=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('economic_publication','club_economics');");
        var count=Convert.ToInt32(exists.ExecuteScalar());if(count==0)return Array.Empty<ClubFinancialResources>();
        if(count!=2)throw new InvalidDataException("Extensión económica incompleta.");
        using(var version=WorldDatabase.Command(c,t,"SELECT version FROM economic_publication WHERE singleton=1;"))
            if(Convert.ToInt32(version.ExecuteScalar())!=1)throw new NotSupportedException("Extensión económica incompatible.");
        using var cmd=WorldDatabase.Command(c,t,"SELECT club_id,season_id,plan_json,cash_cents,transfer_remaining_cents,manager_cost_cents FROM club_economics ORDER BY club_id,season_id;");
        using var r=cmd.ExecuteReader();var result=new List<ClubFinancialResources>();
        while(r.Read())
        {
            var plan=JsonSerializer.Deserialize<ClubEconomicPlan>(r.GetString(2))??throw new InvalidDataException("Plan económico vacío.");plan.Validate();
            if(plan.ClubId!=Guid.Parse(r.GetString(0)))throw new InvalidDataException("Plan de otro club.");
            var cash=r.GetInt64(3)/100m;var remaining=r.GetInt64(4)/100m;decimal? cost=r.IsDBNull(5)?null:r.GetInt64(5)/100m;
            if(remaining>plan.TransferBudget || cost is {} annual && annual!=plan.PlanningManagerEmployerCost)throw new InvalidDataException("Estado económico incoherente.");
            result.Add(plan.Resources(new SeasonId(Guid.Parse(r.GetString(1))),cash,remaining,cost));
        }
        return result.AsReadOnly();
    }
    internal static void CommitManager(SqliteConnection c,SqliteTransaction t,Furvo.Core.Careers.ManagerOffer offer)
    {
        var rows=ReadResources(c,t);if(rows.Count==0)return; // Compatibilidad explícita de carreras creadas desde publicaciones antiguas.
        var resources=rows.Single(x=>x.ClubId==offer.ClubId && x.SeasonId==offer.SeasonId);
        var plan=resources.EconomicPlan!;
        if(resources!=offer.Resources || offer.Terms.Currency!=plan.Currency || offer.Terms.GrossAnnualSalary!=plan.ProposedManagerGrossAnnualSalary)
            throw new InvalidOperationException("La oferta no coincide con los recursos y salario aprobados del club.");
        // Compromiso anual: no es un pago ni reduce tesorería; ya está incluido en personal previsto.
        using var cmd=WorldDatabase.Command(c,t,"UPDATE club_economics SET manager_cost_cents=$cost WHERE club_id=$club AND season_id=$season AND manager_cost_cents IS NULL;",
            ("$cost",Cents(plan.PlanningManagerEmployerCost)),("$club",offer.ClubId.ToString()),("$season",offer.SeasonId.ToString()));
        if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("El club ya tiene un compromiso de entrenador.");
    }
}

/// <summary>Publicación económica v5: copia íntegra de v4 con extensión versionada, sin regenerar jugadores.</summary>
public static class EconomicInitialWorld2026
{
    private static byte[] CatalogBytes()
    {
        using var s=typeof(EconomicInitialWorld2026).Assembly.GetManifestResourceStream("Furvo.Persistence.Data.initial-economy-2026-27.v1.json")??throw new InvalidDataException("Falta economía aprobada.");
        using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();
    }
    public static IReadOnlyList<ClubEconomicPlan> ReadApprovedPlans()
    {
        using var d=JsonDocument.Parse(CatalogBytes());var root=d.RootElement;
        if(root.GetProperty("Status").GetString()!="Approved" || !root.GetProperty("RuntimeUseAllowed").GetBoolean())throw new InvalidDataException("Catálogo económico sin aprobación.");
        var plans=JsonSerializer.Deserialize<ClubEconomicPlan[]>(root.GetProperty("Clubs"))!;
        if(plans.Length!=20 || plans.Select(p=>p.ClubId).Distinct().Count()!=20)throw new InvalidDataException("Se requieren veinte planes distintos.");
        var floor=root.GetProperty("LegalReference").GetProperty("FullTimeGrossAnnualMinimum").GetDecimal();
        foreach(var p in plans){p.Validate();if(p.ProposedManagerGrossAnnualSalary<=floor)throw new InvalidDataException("Salario inferior a referencia legal.");}
        return Array.AsReadOnly(plans);
    }
    public static SqliteWorldStore LoadOrCreate(string sourceV4,string destination)
    {
        var bytes=CatalogBytes();using var doc=JsonDocument.Parse(bytes);var definition=doc.RootElement;var plans=ReadApprovedPlans();
        var sourcePath=WorldDatabase.PathFor(sourceV4);var source=new SqliteWorldStore(sourcePath);var info=source.ReadInfo();var world=source.Load();
        if(info.Kind!=Furvo.Core.Worlds.WorldFileKind.InitialWorld || world.Seasons.Count!=1 || world.Clubs.Count!=20 ||
           Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant()!=definition.GetProperty("SourceSha256").GetString() ||
           !world.Clubs.All(c=>plans.Any(p=>p.ClubId==c.Id.Value && p.Name==c.Name)))throw new InvalidDataException("El origen no coincide con v4.");
        var worldId=new WorldId(definition.GetProperty("WorldId").GetGuid());var catalogHash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var version=definition.GetProperty("Version").GetString()!;var season=world.Seasons[0].Id;
        if(!File.Exists(destination))
        {
            using var origin=WorldDatabase.Open(sourcePath);using var snapshot=origin.BeginTransaction(deferred:true);
            try
            {
                return SqliteWorldFiles.Publish(destination,target=>
                {
                    origin.BackupDatabase(target);using var t=target.BeginTransaction();CareerJson.Migrate(target,t,"Economics001_Initial.sql");
                    using(var metadata=WorldDatabase.Command(target,t,"UPDATE world_info SET world_id=$id; INSERT INTO economic_publication VALUES(1,1,$version,$hash);",("$id",worldId.ToString()),("$version",version),("$hash",catalogHash)))metadata.ExecuteNonQuery();
                    foreach(var plan in plans)
                    {
                        using var row=WorldDatabase.Command(target,t,"INSERT INTO club_economics VALUES($club,$season,$json,$cash,$remaining,NULL);",
                            ("$club",new ClubId(plan.ClubId).ToString()),("$season",season.ToString()),("$json",JsonSerializer.Serialize(plan)),("$cash",SqliteEconomyStore.Cents(plan.InitialCash)),("$remaining",SqliteEconomyStore.Cents(plan.TransferBudget)));
                        row.ExecuteNonQuery();
                    }
                    using var check=WorldDatabase.Command(target,t,"PRAGMA foreign_key_check;");using(var r=check.ExecuteReader())if(r.Read())throw new InvalidDataException("Referencias económicas inválidas.");
                    t.Commit();
                });
            }
            catch(IOException) when(File.Exists(destination)){ /* Otra instancia publicó: verificar sin sobrescribir. */ }
        }
        var existing=new SqliteWorldStore(destination);var stored=SqliteEconomyStore.ReadResources(destination);
        using var c=WorldDatabase.Open(destination);using var tx=c.BeginTransaction(deferred:true);
        using var hash=WorldDatabase.Command(c,tx,"SELECT catalog_sha256 FROM economic_publication WHERE singleton=1;");
        if(existing.ReadInfo().Kind!=Furvo.Core.Worlds.WorldFileKind.InitialWorld || existing.ReadInfo().WorldId!=worldId ||
           InitialSquads2026.Fingerprint(world)!=InitialSquads2026.Fingerprint(existing.Load()) || hash.ExecuteScalar() as string!=catalogHash ||
           stored.Count!=20 || !plans.All(p=>stored.Contains(p.Resources(season,p.InitialCash,p.TransferBudget,null))))throw new InvalidDataException("El destino no coincide con v5; no se sobrescribe.");
        return existing;
    }
}
