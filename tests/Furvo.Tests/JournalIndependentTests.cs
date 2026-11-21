using System.Diagnostics;
using Furvo.Core.Finance;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

public sealed partial class JournalTests
{
    private void Clock(DateOnly date) => Write((c,t) => Sql(c,t,
        "CREATE TABLE IF NOT EXISTS career_state(singleton INTEGER PRIMARY KEY,current_date TEXT); INSERT OR REPLACE INTO career_state(singleton,current_date) VALUES(1,$date)",
        ("$date",date.ToString("yyyy-MM-dd"))));

    [Fact]
    public void IndependentFuturePostingMustBeRejectedBeforePoisoningHistory()
    {
        Clock(Cutoff); Open();
        Write((c,t) => Assert.Throws<InvalidDataException>(() => SqliteJournal.Post(c,t,_career,Charge(1,DateOnly.MaxValue))));
        Write((c,t) => SqliteJournal.Post(c,t,_career,Charge(1)));
        Assert.Equal(9999,Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void IndependentFutureOpeningMustBeRejected()
    {
        Clock(Cutoff);
        Write((c,t) => { SqliteJournal.Migrate(c,t,_career);
            Assert.Throws<InvalidDataException>(() => SqliteJournal.Open(c,t,_career,_club,_season,DateOnly.MaxValue,Guid.NewGuid())); });
        Assert.Null(Reconcile());
    }

    [Fact]
    public void IndependentRollbackMustPreserveOriginalSqliteError()
    {
        Open();
        using var c=Connect(); using var t=c.BeginTransaction();
        Sql(c,t,"CREATE TRIGGER fail_probe BEFORE UPDATE ON journal_probe BEGIN SELECT RAISE(ROLLBACK,'original failure'); END;");
        var error=Assert.Throws<SqliteException>(() => SqliteFinancialOperations.Execute(c,t,
            new(Guid.NewGuid(),_career,"rollback-probe-v1","one","{}"),(db,tx)=>{Sql(db,tx,"UPDATE journal_probe SET value=1");return "{}";}));
        Assert.Contains("original failure",error.Message);
    }

    [Fact]
    public void IndependentSameTokenCompositionRollsBackAndSeparateTokensReplay()
    {
        Open(); var entry=Charge(1); var outer=new FinancialOperationRequest(entry.OperationId,_career,"composed-v1","one","{}");
        Write((c,t)=>Assert.ThrowsAny<Exception>(()=>SqliteFinancialOperations.Execute(c,t,outer,(db,tx)=>{
            SqliteJournal.Post(db,tx,_career,entry);return "{}";})));
        Assert.Equal(10000,Reconcile().JournalCash.Cents);
        outer=outer with{Id=Guid.NewGuid()};
        Write((c,t)=>SqliteFinancialOperations.Execute(c,t,outer,(db,tx)=>{SqliteJournal.Post(db,tx,_career,entry);return "{}";}));
        Write((c,t)=>Assert.True(SqliteFinancialOperations.Execute(c,t,outer,(_,_)=>throw new Exception("must not execute")).Replayed));
        Assert.Equal(9999,Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void IndependentSerializationWhitespaceIsPartOfVersionedTerms()
    {
        Open();var request=new FinancialOperationRequest(Guid.NewGuid(),_career,"wire-v1","one","{\"amount\":1}");
        Write((c,t)=>SqliteFinancialOperations.Execute(c,t,request,(_,_)=>"{}"));
        Write((c,t)=>Assert.Throws<InvalidOperationException>(()=>SqliteFinancialOperations.Execute(c,t,
            request with{Terms="{ \"amount\":1}"},(_,_)=>"{}")));
    }

    [Fact]
    public async Task IndependentDistinctConcurrentChargesCannotOverdraw()
    {
        Open();var entries=new[]{Charge(6000),Charge(6000)};
        var results=await Task.WhenAll(entries.Select(entry=>Task.Run(()=>{
            try{Write((c,t)=>SqliteJournal.Post(c,t,_career,entry));return true;}
            catch(InvalidOperationException){return false;}
            catch(SqliteException){return false;}
        })));
        Assert.Single(results,x=>x);Assert.Equal(4000,Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void IndependentSpentIncomeCannotBeReversedIntoNegativeCash()
    {
        Open();var income=Charge(100) with{Lines=[new(JournalAccount.Cash,EuroAmount.FromCents(100),default),new(JournalAccount.Income,default,EuroAmount.FromCents(100))]};
        Write((c,t)=>SqliteJournal.Post(c,t,_career,income));Write((c,t)=>SqliteJournal.Post(c,t,_career,Charge(10100)));
        var reverse=Charge(100) with{Reverses=income.Id,Lines=income.Lines.Select(l=>new JournalLine(l.Account,l.Credit,l.Debit)).ToArray()};
        Write((c,t)=>Assert.ThrowsAny<Exception>(()=>SqliteJournal.Post(c,t,_career,reverse)));
        Assert.Equal(0,Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void IndependentMeasureGrowingJournal()
    {
        Open();using var c=Connect();using var t=c.BeginTransaction();var watch=Stopwatch.StartNew();
        foreach(var n in Enumerable.Range(1,1000)){
            SqliteJournal.Post(c,t,_career,Charge(1));
            if(n is 100 or 500 or 1000) Console.WriteLine($"JOURNAL_BENCH n={n} elapsed_ms={watch.ElapsedMilliseconds}");
        }
        t.Commit();Assert.Equal(9000,Reconcile().JournalCash.Cents);
    }
}
