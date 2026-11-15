using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Información contractual asociada por FK. Solo migración/escritura explícita; las consultas nunca crean tablas.</summary>
public sealed class SqliteContractReleaseStore(string path)
{
    internal static bool HasSchema(SqliteConnection c,SqliteTransaction t)
    {
        using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='contract_release_schema'");if(Convert.ToInt32(q.ExecuteScalar())==0)return false;
        q.CommandText="SELECT version FROM contract_release_schema WHERE singleton=1";if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de información de rescisión incompatible.");return true;
    }
    private static void RequireSave(SqliteConnection c,SqliteTransaction t){if(WorldDatabase.Info(c,t).Kind!=WorldFileKind.Save)throw new InvalidOperationException("No se modifica una publicación inicial.");}
    public void Migrate(){using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();RequireSave(c,t);if(!HasSchema(c,t))CareerJson.Migrate(c,t,"ContractRelease001_Information.sql");t.Commit();}
    public void Record(ContractReleaseClause clause)
    {
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();RequireSave(c,t);
        var contract=SqliteWorldStore.Load(c,t).Contracts.SingleOrDefault(k=>k.Id==clause.ContractId)??throw new KeyNotFoundException("Contrato no disponible en esta partida.");Write(c,t,clause,contract);t.Commit();
    }
    internal static void Write(SqliteConnection c,SqliteTransaction t,ContractReleaseClause clause,Contract contract)
    {
        RequireSave(c,t);ContractReleaseRules.Validate(clause,contract);
        foreach(var condition in clause.Conditions??Array.Empty<ReleaseClauseCondition>()){
            var table=condition.Kind==ReleaseConditionKind.BuyerClub?"clubs":"competitions";using var check=WorldDatabase.Command(c,t,"SELECT count(*) FROM "+table+" WHERE id=$id",("$id",condition.Reference.ToString("D")));if(Convert.ToInt32(check.ExecuteScalar())!=1)throw new InvalidDataException("Referencia de condición contractual inexistente.");
        }
        if(!HasSchema(c,t))CareerJson.Migrate(c,t,"ContractRelease001_Information.sql");
        using var q=WorldDatabase.Command(c,t,"INSERT INTO contract_release_terms VALUES($id,$presence,$amount,$currency,$from,$until,$scope,$conditions,$disclosure,$evidence,$confirmed) ON CONFLICT(contract_id) DO UPDATE SET presence=excluded.presence,amount_minor=excluded.amount_minor,currency=excluded.currency,valid_from=excluded.valid_from,valid_until=excluded.valid_until,scope=excluded.scope,conditions_json=excluded.conditions_json,disclosure=excluded.disclosure,evidence=excluded.evidence,confirmed_on=excluded.confirmed_on;",
            ("$id",clause.ContractId.ToString()),("$presence",(int)clause.Presence),("$amount",clause.Amount is {} amount?(object)checked((long)(amount*100)):null),("$currency",clause.Currency),("$from",clause.From?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$until",clause.Until?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$scope",(int)clause.Scope),("$conditions",CareerJson.Write(clause.Conditions??Array.Empty<ReleaseClauseCondition>())),("$disclosure",(int)clause.Disclosure),("$evidence",clause.Evidence),("$confirmed",clause.ConfirmedOn?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)));q.ExecuteNonQuery();
    }
    internal static IReadOnlyDictionary<ContractId,ContractReleaseClause> Read(SqliteConnection c,SqliteTransaction t)
    {
        if(!HasSchema(c,t))return new Dictionary<ContractId,ContractReleaseClause>();
        using var q=WorldDatabase.Command(c,t,"SELECT contract_id,presence,amount_minor,currency,valid_from,valid_until,scope,conditions_json,disclosure,evidence,confirmed_on FROM contract_release_terms ORDER BY contract_id");using var r=q.ExecuteReader();var result=new Dictionary<ContractId,ContractReleaseClause>();
        DateOnly? Day(int i)=>r.IsDBNull(i)?null:DateOnly.ParseExact(r.GetString(i),"yyyy-MM-dd",CultureInfo.InvariantCulture);
        while(r.Read()){var id=new ContractId(Guid.Parse(r.GetString(0)));result.Add(id,new(id,(ReleaseClausePresence)r.GetInt32(1),r.IsDBNull(2)?null:r.GetInt64(2)/100m,r.IsDBNull(3)?null:r.GetString(3),Day(4),Day(5),(ReleaseClauseScope)r.GetInt32(6),CareerJson.Read<ReleaseClauseCondition[]>(r.GetString(7)),(ReleaseClauseDisclosure)r.GetInt32(8),r.IsDBNull(9)?null:r.GetString(9),Day(10)));}return result;
    }
    public PlayerContractInfo Player(PlayerId id)
    {
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var owner=SqliteLineupStore.Owner(c,t);var world=SqliteWorldStore.Load(c,t);var contract=Select(world.Contracts,id,owner.Date);
        var club=contract is null?null:world.Clubs.Single(k=>k.Id==contract.ClubId);
        return new(contract?.Id,club?.Name,contract?.EndDate,ContractInformation.Release(contract,new(owner.Date,owner.Club,club?.Country)));
    }
    internal static Contract? Select(IEnumerable<Contract> contracts,PlayerId player,DateOnly date)=>contracts.Where(c=>c.PlayerId==player).OrderByDescending(c=>c.StartDate is {} start&&start<=date&&c.EndDate>=date).ThenByDescending(c=>c.StartDate).ThenBy(c=>c.Id.ToString(),StringComparer.Ordinal).FirstOrDefault();
}
