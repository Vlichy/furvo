using System.Globalization;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Finance;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Autorizaciones y compromisos por carrera; nunca escribe caja, contratos ni diario.</summary>
public static class SqliteBudgetStore
{
    private static bool Installed(SqliteConnection c,SqliteTransaction t)
    {
        using var q=WorldDatabase.Command(c,t,"""
            SELECT count(*) FROM sqlite_master WHERE
            (type='table' AND name IN ('budget_schema','budgets','budget_revisions')) OR
            (type='trigger' AND name IN ('budgets_no_overlap','budget_revision_order','budgets_no_update','budgets_no_delete','budget_revisions_no_update','budget_revisions_no_delete'))
            """);
        var count=Convert.ToInt32(q.ExecuteScalar());if(count==0)return false;
        if(count!=9)throw new InvalidDataException("Extensión presupuestaria incompleta.");
        q.CommandText="SELECT version FROM budget_schema WHERE singleton=1";
        if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión presupuestaria no compatible.");
        if(!SqliteFinancialOperations.HasSchema(c,t))throw new InvalidDataException("Faltan operaciones financieras.");
        return true;
    }

    public static void Migrate(SqliteConnection c,SqliteTransaction t,SaveId career)
    {
        SqliteFinancialOperations.Migrate(c,t,career);
        if(!Installed(c,t))CareerJson.Migrate(c,t,"Budgets001_Initial.sql");
    }

    public static BudgetSnapshot? Read(SqliteConnection c,SqliteTransaction t,SaveId career,Guid id)
        => History(c,t,career,id).LastOrDefault();

    public static IReadOnlyList<BudgetSnapshot> History(SqliteConnection c,SqliteTransaction t,SaveId career,Guid id)
    {
        SqliteFinancialOperations.CheckCareer(c,t,career);if(!Installed(c,t))return [];
        using var q=WorldDatabase.Command(c,t,"""
            SELECT r.snapshot_json,r.revision,r.authorized_cents,r.reserved_cents,r.committed_cents,r.executed_cents,b.definition_json
            FROM budget_revisions r JOIN budgets b ON b.id=r.budget_id WHERE r.budget_id=$id ORDER BY r.revision
            """,("$id",id.ToString("D")));
        using var reader=q.ExecuteReader();var result=new List<BudgetSnapshot>();
        while(reader.Read()){
            var state=CareerJson.Read<BudgetSnapshot>(reader.GetString(0));state.Validate();
            // La proyección tipada y el documento inmutable deben describir la misma revisión.
            if(state.Definition.Id!=id || state.Revision!=reader.GetInt32(1) || state.Revision!=result.Count+1 ||
                state.AuthorizedCents!=reader.GetInt64(2) || state.ReservedCents!=reader.GetInt64(3) ||
                state.CommittedCents!=reader.GetInt64(4) || state.ExecutedCents!=reader.GetInt64(5) ||
                state.Definition!=CareerJson.Read<BudgetDefinition>(reader.GetString(6)))
                throw new InvalidDataException("La revisión presupuestaria no concuerda con su historial o proyección.");
            result.Add(state);
        }
        return result.AsReadOnly();
    }

    public static IReadOnlyList<BudgetSnapshot> List(SqliteConnection c,SqliteTransaction t,SaveId career,ClubId club)
    {
        SqliteFinancialOperations.CheckCareer(c,t,career);if(!Installed(c,t))return [];
        using var q=WorldDatabase.Command(c,t,"SELECT id FROM budgets WHERE club_id=$club ORDER BY start_date,category,id",("$club",club.ToString()));
        var ids=new List<Guid>();using(var reader=q.ExecuteReader())while(reader.Read())ids.Add(Guid.Parse(reader.GetString(0)));
        return ids.Select(id=>Read(c,t,career,id)!).ToArray();
    }

    // Contrato de huella v1 explícito: arrays de campos primitivos en orden fijo, independiente del DTO de dominio.
    private static object[] DefinitionTerms(BudgetDefinition d)=>[d.Id.ToString("D"),d.Club.ToString(),d.Category,d.Basis,Date(d.From),Date(d.Until),d.Currency,
        d.Source.Reference,d.Source.Version,Date(d.Source.ReferenceDate),(int)d.Source.Kind,d.Source.Method];

    public static FinancialOperationResult Create(SqliteConnection c,SqliteTransaction t,SaveId career,Guid operation,
        string origin,BudgetDefinition definition,EuroAmount amount,DateOnly date,string actor,string authority,string reason,IBudgetAuthorization permission)
    {
        ArgumentNullException.ThrowIfNull(definition);definition.Validate();EuroAmount.FromCents(amount.Cents);
        var terms=JsonSerializer.Serialize(new object[]{DefinitionTerms(definition),amount.Cents,Date(date),actor,authority,reason});
        return SqliteFinancialOperations.Execute(c,t,new(operation,career,"budget-create-v1",origin,terms),(db,tx)=>{
            CheckWrite(db,tx,date,actor,authority,reason);Authorize(permission,career,definition.Club,actor,authority,BudgetAction.Authorize);
            using var insert=WorldDatabase.Command(db,tx,"INSERT INTO budgets VALUES($id,$club,$category,$basis,$start,$end,$currency,$json)",
                ("$id",definition.Id.ToString("D")),("$club",definition.Club.ToString()),("$category",definition.Category),("$basis",definition.Basis),
                ("$start",Date(definition.From)),("$end",Date(definition.Until)),("$currency",definition.Currency),("$json",CareerJson.Write(definition)));
            insert.ExecuteNonQuery();var state=new BudgetSnapshot(definition,1,amount.Cents,[]);state.Validate();
            InsertRevision(db,tx,state,operation,date,actor,authority,reason,BudgetAction.Authorize);
            return JsonSerializer.Serialize(new{Budget=definition.Id,Revision=1});
        });
    }

    /// <summary>Una operación puede afectar a varios periodos; todos se confirman o revierten juntos.</summary>
    public static FinancialOperationResult Apply(SqliteConnection c,SqliteTransaction t,SaveId career,Guid operation,string origin,
        IReadOnlyList<BudgetChange> changes,DateOnly date,string actor,string authority,string reason,IBudgetAuthorization permission)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var frozen=changes.ToArray();
        if(frozen.Length==0 || frozen.Any(x=>x is null) || frozen.Select(x=>x.Budget).Distinct().Count()!=frozen.Length)
            throw new InvalidDataException("Indica una modificación por presupuesto y operación.");
        var payload=frozen.Select(x=>new object[]{x.Budget.ToString("D"),x.ExpectedRevision,(int)x.Action,x.Allocation.ToString("D"),x.Origin,x.Amount.Cents}).ToArray();
        var terms=JsonSerializer.Serialize(new object[]{payload,Date(date),actor,authority,reason});
        return SqliteFinancialOperations.Execute(c,t,new(operation,career,"budget-change-v1",origin,terms),(db,tx)=>{
            CheckWrite(db,tx,date,actor,authority,reason);var results=new List<object>();
            foreach(var change in frozen){
                var state=Read(db,tx,career,change.Budget)??throw new InvalidOperationException("Presupuesto no encontrado; no se presume disponible.");
                Authorize(permission,career,state.Definition.Club,actor,authority,change.Action);
                var next=state.Apply(change);InsertRevision(db,tx,next,operation,date,actor,authority,reason,change.Action);
                results.Add(new{Budget=change.Budget,Revision=next.Revision});
            }
            return JsonSerializer.Serialize(results);
        });
    }

    private static void Authorize(IBudgetAuthorization permission,SaveId career,ClubId club,string actor,string authority,BudgetAction action)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if(!permission.Allows(career,club,actor,authority,action))throw new InvalidOperationException("No existe autorización presupuestaria para esta operación.");
    }

    private static void CheckWrite(SqliteConnection c,SqliteTransaction t,DateOnly date,string actor,string authority,string reason)
    {
        if(!Installed(c,t))throw new InvalidOperationException("Instala explícitamente la extensión presupuestaria.");
        if(date==default || string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(reason))
            throw new InvalidDataException("Se requieren fecha, actor, autorización y motivo.");
        using var q=WorldDatabase.Command(c,t,"SELECT s.current_date FROM career_state s WHERE singleton=1");
        if(q.ExecuteScalar() is not string value || !DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var today) || date!=today)
            throw new InvalidOperationException("La operación debe usar la fecha efectiva de la carrera.");
    }

    private static void InsertRevision(SqliteConnection c,SqliteTransaction t,BudgetSnapshot state,Guid operation,DateOnly date,
        string actor,string authority,string reason,BudgetAction action)
    {
        using var q=WorldDatabase.Command(c,t,"INSERT INTO budget_revisions VALUES($id,$revision,$operation,$date,$actor,$authority,$reason,$action,$authorized,$reserved,$committed,$executed,$json)",
            ("$id",state.Definition.Id.ToString("D")),("$revision",state.Revision),("$operation",operation.ToString("D")),("$date",Date(date)),
            ("$actor",actor),("$authority",authority),("$reason",reason),("$action",(int)action),("$authorized",state.AuthorizedCents),
            ("$reserved",state.ReservedCents),("$committed",state.CommittedCents),("$executed",state.ExecutedCents),("$json",CareerJson.Write(state)));
        q.ExecuteNonQuery();
    }
    private static string Date(DateOnly date)=>date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
}
