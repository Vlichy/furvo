using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Finance;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Importes sintéticos y carreras aisladas; no se crean datos financieros reales de clubes.</summary>
public sealed class BudgetTests : IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"FurvoBudgetTests",Guid.NewGuid().ToString("N"));
    private readonly SaveId _career=SaveId.New();
    private readonly ClubId _club;
    private readonly DateOnly _date=new(2026,7,1);
    private string FilePath=>Path.Combine(_root,"career.db");
    private sealed class Permission(bool allowed=true):IBudgetAuthorization
    {public bool Allows(SaveId career,ClubId club,string actor,string authority,BudgetAction action)=>allowed && actor=="fixture-manager" && authority=="fixture-board";}
    public BudgetTests()
    {
        Directory.CreateDirectory(_root);File.Copy(Path.Combine(AppContext.BaseDirectory,"published-v5.db"),FilePath);
        using var c=Connect();using var q=c.CreateCommand();
        q.CommandText="UPDATE world_info SET kind='save'; INSERT INTO save_metadata VALUES(1,$id); CREATE TABLE career_state(singleton INTEGER PRIMARY KEY,current_date TEXT); INSERT INTO career_state VALUES(1,'2026-07-01');";
        q.Parameters.AddWithValue("$id",_career.ToString());q.ExecuteNonQuery();
        _club=SqliteEconomyStore.ReadResources(FilePath)[0].ClubId;
    }
    private SqliteConnection Connect(){var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=FilePath,Mode=SqliteOpenMode.ReadWrite,ForeignKeys=true,Pooling=false}.ToString());c.Open();return c;}
    private void Write(Action<SqliteConnection,SqliteTransaction> action){using var c=Connect();using var t=c.BeginTransaction();action(c,t);t.Commit();}
    private static void Sql(SqliteConnection c,SqliteTransaction t,string sql){using var q=c.CreateCommand();q.Transaction=t;q.CommandText=sql;q.ExecuteNonQuery();}
    private BudgetDefinition Definition(string category="transfers",int year=2026)=>new(Guid.NewGuid(),_club,category,"fixed-commitment",new(year,7,1),new(year+1,6,30),"EUR",new("synthetic-fixture","v1",_date,FinancialEvidenceKind.Estimated,"Explicit test values"));
    private BudgetDefinition Create(long cents=10000,BudgetDefinition? definition=null)
    {
        var d=definition??Definition();Write((c,t)=>{SqliteBudgetStore.Migrate(c,t,_career);SqliteBudgetStore.Create(c,t,_career,Guid.NewGuid(),"create/"+d.Id,d,EuroAmount.FromCents(cents),_date,"fixture-manager","fixture-board","Initial fixture",new Permission());});return d;
    }
    private BudgetSnapshot Read(Guid id){using var c=Connect();using var t=c.BeginTransaction(deferred:true);return SqliteBudgetStore.Read(c,t,_career,id)!;}
    private void Apply(BudgetChange change,Guid? token=null,string? origin=null)=>Write((c,t)=>SqliteBudgetStore.Apply(c,t,_career,token??Guid.NewGuid(),origin??Guid.NewGuid().ToString(),[change],_date,"fixture-manager","fixture-board","Fixture decision",new Permission()));
    private static BudgetChange Change(BudgetSnapshot s,BudgetAction action,long cents,Guid id=default,string origin="")=>new(s.Definition.Id,s.Revision,action,id,origin,EuroAmount.FromCents(cents));

    [Fact] public void CreationPreservesCashAndDoesNotInstallOrWriteJournal()
    {
        var before=SqliteEconomyStore.ReadResources(FilePath);var d=Create();var s=Read(d.Id);
        Assert.Equal(10000,s.Available.Cents);Assert.Equal(before,SqliteEconomyStore.ReadResources(FilePath));
        using var c=Connect();using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM sqlite_master WHERE name='journal_entries'";Assert.Equal(0L,q.ExecuteScalar());
        Assert.Equal(d.Source,s.Definition.Source);
    }

    [Fact] public void ReserveConfirmExecuteAndReleaseNeverDoubleConsume()
    {
        var d=Create();var id=Guid.NewGuid();Apply(Change(Read(d.Id),BudgetAction.Reserve,3000,id,"contract/1"));
        Assert.Equal(7000,Read(d.Id).Available.Cents);Assert.Equal(3000,Read(d.Id).ReservedCents);
        Apply(Change(Read(d.Id),BudgetAction.Confirm,3000,id,"contract/1"));
        var s=Read(d.Id);Assert.Equal(0,s.ReservedCents);Assert.Equal(3000,s.CommittedCents);Assert.Equal(7000,s.Available.Cents);
        Apply(Change(s,BudgetAction.Execute,1000,id,"contract/1"));Assert.Equal(7000,Read(d.Id).Available.Cents);
        Apply(Change(Read(d.Id),BudgetAction.Release,2000,id,"contract/1"));s=Read(d.Id);
        Assert.Equal(1000,s.ExecutedCents);Assert.Equal(1000,s.CommittedCents);Assert.Equal(9000,s.Available.Cents);
        Assert.Throws<InvalidOperationException>(()=>Apply(Change(s,BudgetAction.Release,1,id,"contract/1")));
    }

    [Fact] public void CancelledReservationCannotBeReleasedOrRecreatedAgain()
    {
        var d=Create();var id=Guid.NewGuid();Apply(Change(Read(d.Id),BudgetAction.Reserve,100,id,"one"));
        Apply(Change(Read(d.Id),BudgetAction.Release,100,id,"one"));var s=Read(d.Id);
        Assert.Equal(10000,s.Available.Cents);
        Assert.Throws<InvalidOperationException>(()=>Apply(Change(s,BudgetAction.Release,100,id,"one")));
        Assert.Throws<InvalidOperationException>(()=>Apply(Change(s,BudgetAction.Commit,100,Guid.NewGuid(),"one")));
    }

    [Fact] public void AuthorizationReductionExposesDeficitWithoutErasingCommitment()
    {
        var d=Create();Apply(Change(Read(d.Id),BudgetAction.Commit,9000,Guid.NewGuid(),"fixed"));
        Apply(Change(Read(d.Id),BudgetAction.Authorize,8000));var s=Read(d.Id);
        Assert.Equal(-1000,s.Available.Cents);Assert.Equal(9000,s.CommittedCents);
        Assert.Throws<InvalidOperationException>(()=>Apply(Change(s,BudgetAction.Commit,1,Guid.NewGuid(),"extra")));
        Apply(Change(s,BudgetAction.Authorize,12000));Assert.Equal(3000,Read(d.Id).Available.Cents);
    }

    [Fact] public void AmendmentRetainsExecutedPartAndRequiresCapacity()
    {
        var d=Create();var id=Guid.NewGuid();Apply(Change(Read(d.Id),BudgetAction.Commit,1000,id,"one"));
        Apply(Change(Read(d.Id),BudgetAction.Execute,500,id,"one"));var s=Read(d.Id);
        Assert.Throws<InvalidOperationException>(()=>Apply(Change(s,BudgetAction.Amend,499,id,"one")));
        Assert.Throws<InvalidOperationException>(()=>Apply(Change(s,BudgetAction.Amend,10001,id,"one")));
        Apply(Change(s,BudgetAction.Amend,1500,id,"one"));Assert.Equal(8500,Read(d.Id).Available.Cents);
    }

    [Fact] public void ReplayAfterReopeningAndConflictingTermsAreSafe()
    {
        var d=Create();var change=Change(Read(d.Id),BudgetAction.Commit,123,Guid.NewGuid(),"one");var token=Guid.NewGuid();Apply(change,token,"command/1");
        Write((c,t)=>Assert.True(SqliteBudgetStore.Apply(c,t,_career,token,"command/1",[change],_date,"fixture-manager","fixture-board","Fixture decision",new Permission()).Replayed));
        Assert.Throws<InvalidOperationException>(()=>Apply(change with{Amount=EuroAmount.FromCents(124)},token,"command/1"));
        Assert.Throws<InvalidOperationException>(()=>Apply(change,Guid.NewGuid(),"command/1"));
        Assert.Equal(9877,Read(d.Id).Available.Cents);Assert.Equal(2,Read(d.Id).Revision);
    }

    [Fact] public async Task ConcurrentOperationsCannotSpendTheSameCapacity()
    {
        var d=Create();var s=Read(d.Id);
        var outcomes=await Task.WhenAll(Enumerable.Range(0,2).Select(i=>Task.Run(()=>{
            try{Apply(Change(s,BudgetAction.Commit,6000,Guid.NewGuid(),"contract/"+i));return true;}
            catch(InvalidOperationException){return false;}
        })));
        Assert.Single(outcomes,x=>x);Assert.Equal(4000,Read(d.Id).Available.Cents);
    }

    [Fact] public void MultiPeriodFailureRollsBackEarlierRevisionAndReceipt()
    {
        var first=Create();var second=Create(100,Definition(year:2027));var token=Guid.NewGuid();
        Write((c,t)=>{
            Assert.Throws<InvalidOperationException>(()=>SqliteBudgetStore.Apply(c,t,_career,token,"multi",[
                Change(Read(first.Id),BudgetAction.Commit,1000,Guid.NewGuid(),"one"),
                Change(Read(second.Id),BudgetAction.Commit,101,Guid.NewGuid(),"two")],_date,"fixture-manager","fixture-board","Fixture",new Permission()));
        });
        Assert.Equal(1,Read(first.Id).Revision);Assert.Equal(1,Read(second.Id).Revision);
        using var db=Connect();using var q=db.CreateCommand();q.CommandText="SELECT count(*) FROM financial_operations WHERE id=$id";q.Parameters.AddWithValue("$id",token.ToString("D"));Assert.Equal(0L,q.ExecuteScalar());
    }

    [Fact] public void OuterRollbackRestoresBudget()
    {
        var d=Create();using(var c=Connect())using(var t=c.BeginTransaction()){
            SqliteBudgetStore.Apply(c,t,_career,Guid.NewGuid(),"rollback",[Change(Read(d.Id),BudgetAction.Reserve,100,Guid.NewGuid(),"one")],_date,"fixture-manager","fixture-board","Fixture",new Permission());t.Rollback();}
        Assert.Equal(1,Read(d.Id).Revision);
    }

    [Fact] public void SqlFailureCannotLeavePartialMultiBudgetOperation()
    {
        var a=Create();var b=Create(1000,Definition(year:2027));
        Write((c,t)=>{
            Sql(c,t,$"CREATE TRIGGER budget_fail BEFORE INSERT ON budget_revisions WHEN NEW.budget_id='{b.Id:D}' AND NEW.revision=2 BEGIN SELECT RAISE(ABORT,'injected failure'); END;");
            Assert.Throws<SqliteException>(()=>SqliteBudgetStore.Apply(c,t,_career,Guid.NewGuid(),"fault",[
                Change(Read(a.Id),BudgetAction.Commit,1,Guid.NewGuid(),"a"),Change(Read(b.Id),BudgetAction.Commit,1,Guid.NewGuid(),"b")],_date,"fixture-manager","fixture-board","Fixture",new Permission()));
        });Assert.Equal(1,Read(a.Id).Revision);Assert.Equal(1,Read(b.Id).Revision);
    }

    [Fact] public void MissingExtensionReadIsNonMutatingAndMigrationRollbackPreservesFile()
    {
        var hash=SHA256.HashData(File.ReadAllBytes(FilePath));
        using(var c=Connect())using(var t=c.BeginTransaction()){
            Assert.Null(SqliteBudgetStore.Read(c,t,_career,Guid.NewGuid()));SqliteBudgetStore.Migrate(c,t,_career);t.Rollback();}
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(FilePath)));
    }

    [Fact] public void PeriodsDoNotOverlapOrAutomaticallyCarryCapacity()
    {
        var a=Create();Assert.Throws<SqliteException>(()=>Create(100,Definition()));
        var b=Create(200,Definition(year:2027));Assert.Equal(200,Read(b.Id).Available.Cents);Assert.Equal(10000,Read(a.Id).Available.Cents);
    }

    [Fact] public void AuthorityCannotBeInventedByAnActorLabel()
    {
        var d=Create();Write((c,t)=>Assert.Throws<InvalidOperationException>(()=>SqliteBudgetStore.Apply(c,t,_career,Guid.NewGuid(),"no-permission",
            [Change(Read(d.Id),BudgetAction.Authorize,999999)],_date,"fixture-manager","fixture-board","Fixture",new Permission(false))));
        Assert.Equal(10000,Read(d.Id).AuthorizedCents);
    }

    [Fact] public void FutureOperationDateAndForeignCareerAreRejected()
    {
        var d=Create();Write((c,t)=>{
            Assert.Throws<InvalidOperationException>(()=>SqliteBudgetStore.Read(c,t,SaveId.New(),d.Id));
            Assert.Throws<InvalidOperationException>(()=>SqliteBudgetStore.Apply(c,t,_career,Guid.NewGuid(),"future",[Change(Read(d.Id),BudgetAction.Authorize,1)],_date.AddDays(1),"fixture-manager","fixture-board","Fixture",new Permission()));
        });Assert.Equal(1,Read(d.Id).Revision);
    }

    [Fact] public void SqliteHistoryIsImmutableAndReferencesRemainValid()
    {
        var d=Create();Apply(Change(Read(d.Id),BudgetAction.Commit,123,Guid.NewGuid(),"one"));
        Write((c,t)=>{
            Assert.Throws<SqliteException>(()=>Sql(c,t,"DELETE FROM budget_revisions"));
            Assert.Throws<SqliteException>(()=>Sql(c,t,"UPDATE budgets SET currency='USD'"));
            using var q=c.CreateCommand();q.Transaction=t;q.CommandText="PRAGMA integrity_check";Assert.Equal("ok",q.ExecuteScalar());q.CommandText="PRAGMA foreign_key_check";Assert.Null(q.ExecuteScalar());
        });Assert.Equal(123,Read(d.Id).CommittedCents);
    }

    [Fact] public void MoneyUsesExactCentsAndTechnicalLimits()
    {
        Assert.Equal(123,EuroAmount.FromEuros(1.23m).Cents);
        Assert.Throws<InvalidDataException>(()=>EuroAmount.FromEuros(1.001m));Assert.Throws<ArgumentOutOfRangeException>(()=>EuroAmount.FromEuros(-1));
        var d=Create(long.MaxValue);Apply(Change(Read(d.Id),BudgetAction.Commit,long.MaxValue,Guid.NewGuid(),"max"));
        Assert.Equal(0,Read(d.Id).Available.Cents);
        Assert.Throws<OverflowException>(()=>Apply(Change(Read(d.Id),BudgetAction.Commit,1,Guid.NewGuid(),"overflow")));
    }

    [Fact] public void SourceAndCurrencyAreRequiredWithoutGuessingClubFinances()
    {
        Assert.Throws<InvalidDataException>(()=>Create(1,Definition() with{Currency="USD"}));
        var d=Definition();Assert.Throws<InvalidDataException>(()=>Create(1,d with{Source=d.Source with{Method=""}}));
        Assert.Throws<InvalidDataException>(()=>Create(1,d with{Source=d.Source with{Reference=""}}));
    }

    [Fact] public void InstalledButPartialSchemaIsNotRepairedSilently()
    {
        Create();Write((c,t)=>Sql(c,t,"DROP TRIGGER budget_revision_order"));
        Write((c,t)=>Assert.Throws<InvalidDataException>(()=>SqliteBudgetStore.Migrate(c,t,_career)));
    }

    [Fact] public void ExistingJournalAndLegacyResourcesRemainUnchanged()
    {
        var resources=SqliteEconomyStore.ReadResources(FilePath);var source=resources.Single(r=>r.ClubId==_club);
        Write((c,t)=>{SqliteJournal.Migrate(c,t,_career);SqliteJournal.Open(c,t,_career,_club,source.SeasonId,_date,Guid.NewGuid());});
        var d=Create();Apply(Change(Read(d.Id),BudgetAction.Reserve,333,Guid.NewGuid(),"reserved"));
        Assert.Equal(resources,SqliteEconomyStore.ReadResources(FilePath));
        using var c=Connect();using var t=c.BeginTransaction();
        Assert.Single(SqliteJournal.Movements(c,t,_career,_club));
        var reconciliation=SqliteJournal.Reconcile(c,t,_career,_club)!;Assert.True(reconciliation.Balanced);Assert.False(reconciliation.LegacyChanged);
    }

    [Fact] public void CreationReplayAndUnknownBudgetDoNotInventCapacity()
    {
        var d=Definition();var token=Guid.NewGuid();Write((c,t)=>{
            SqliteBudgetStore.Migrate(c,t,_career);
            var first=SqliteBudgetStore.Create(c,t,_career,token,"create",d,EuroAmount.FromCents(100),_date,"fixture-manager","fixture-board","Fixture",new Permission());
            var retry=SqliteBudgetStore.Create(c,t,_career,token,"create",d,EuroAmount.FromCents(100),_date,"fixture-manager","fixture-board","Fixture",new Permission());
            Assert.True(retry.Replayed);Assert.Equal(first.ResultJson,retry.ResultJson);
            Assert.Null(SqliteBudgetStore.Read(c,t,_career,Guid.NewGuid()));
        });Assert.Equal(1,Read(d.Id).Revision);
    }

    [Fact] public void ExplicitMultiPeriodCommitmentPreservesIdentityWithoutCarryingCash()
    {
        var a=Create();var b=Create(5000,Definition(year:2027));var allocation=Guid.NewGuid();
        var changes=new[]{Change(Read(a.Id),BudgetAction.Commit,1000,allocation,"contract/annual"),Change(Read(b.Id),BudgetAction.Commit,2000,allocation,"contract/annual")};
        Write((c,t)=>SqliteBudgetStore.Apply(c,t,_career,Guid.NewGuid(),"contract/annual/revision1",changes,_date,"fixture-manager","fixture-board","Explicit allocations",new Permission()));
        Assert.Equal(9000,Read(a.Id).Available.Cents);Assert.Equal(3000,Read(b.Id).Available.Cents);
        Assert.Equal(allocation,Assert.Single(Read(a.Id).Allocations).Id);Assert.Equal(allocation,Assert.Single(Read(b.Id).Allocations).Id);
    }

    [Fact] public void InstalledExtensionWithoutDataPreservesOldSaveAndRejectsUnknownVersion()
    {
        var before=SqliteEconomyStore.ReadResources(FilePath);
        Write((c,t)=>{SqliteBudgetStore.Migrate(c,t,_career);SqliteBudgetStore.Migrate(c,t,_career);Assert.Empty(SqliteBudgetStore.List(c,t,_career,_club));});
        Assert.Equal(before,SqliteEconomyStore.ReadResources(FilePath));
        Write((c,t)=>Sql(c,t,"DROP TABLE budget_schema; CREATE TABLE budget_schema(singleton INTEGER PRIMARY KEY,version INTEGER); INSERT INTO budget_schema VALUES(1,99)"));
        Write((c,t)=>Assert.Throws<NotSupportedException>(()=>SqliteBudgetStore.Migrate(c,t,_career)));
    }

    [Fact] public void SeparateClubsHaveSeparateCapacity()
    {
        var other=SqliteEconomyStore.ReadResources(FilePath).First(r=>r.ClubId!=_club).ClubId;
        var a=Create();var b=Create(1,Definition() with{Club=other});Apply(Change(Read(a.Id),BudgetAction.Commit,10000,Guid.NewGuid(),"one"));
        Assert.Equal(1,Read(b.Id).Available.Cents);
    }

    [Fact] public void DomainRejectsInvalidConfirmationAndNegativeSignedMoney()
    {
        var d=Create();var s=Read(d.Id);var id=Guid.NewGuid();
        Assert.Throws<ArgumentOutOfRangeException>(()=>s.Apply(new(d.Id,s.Revision,BudgetAction.Reserve,id,"one",EuroAmount.FromCents(-1,MoneySign.Signed))));
        Apply(Change(s,BudgetAction.Reserve,100,id,"one"));s=Read(d.Id);
        Assert.Throws<InvalidOperationException>(()=>s.Apply(Change(s,BudgetAction.Confirm,99,id,"one")));
        Assert.Throws<InvalidOperationException>(()=>s.Apply(Change(s,BudgetAction.Execute,1,id,"one")));
        Assert.Equal(100,Read(d.Id).ReservedCents);
    }

    public void Dispose()=>Directory.Delete(_root,true);
}
