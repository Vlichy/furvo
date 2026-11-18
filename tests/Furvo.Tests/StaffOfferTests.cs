using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

public sealed partial class CareerCreationTests
{
    [Fact]
    public void HistoricalStaffDraftRequiresExplicitDateCorrectionBeforeSending()
    {
        var (careers, file, _) = TacticalCareer();
        var save = new SaveId(Guid.Parse(Path.GetFileNameWithoutExtension(file)));
        var career = careers.Load(save);
        var person = SqliteStaffDirectory.Read(file,career).First(p => p.Employment is not null && p.Club != career.ClubId);
        var team = new SqliteClubSquadStore(file).Teams().First(t => t.Category != "First");
        var start = career.CurrentDate;
        var terms = new StaffOfferTerms(team.Id,"AcademyCoach",start,StaffOfferRules.LastContractDay(start,1),
            1_000_000m,"EUR",0,0,[]);
        var draft = new SqliteStaffOffers(file,career).Draft(person.Id,terms);

        using (var connection = Connection(file))
        using (var command = connection.CreateCommand())
        {
            // La versión actual congela el reloj. Solo esta copia de prueba simula su avance.
            command.CommandText = "DROP TRIGGER frozen_career_start; UPDATE career_state SET current_date = $date WHERE singleton = 1";
            command.Parameters.AddWithValue("$date",start.AddDays(1).ToString("yyyy-MM-dd"));
            Assert.Equal(1,command.ExecuteNonQuery());
        }

        career = careers.Load(save);
        var store = new SqliteStaffOffers(file,career);
        var original = Assert.Single(store.History(draft.Id));
        Assert.Equal(start,original.Terms.Start);
        Assert.Contains("incorporación",Assert.Throws<InvalidDataException>(() => store.Move(original,"Send")).Message);
        Assert.Throws<InvalidDataException>(() => store.Move(original,"Revise",original.Terms));
        Assert.Single(store.History(draft.Id));

        var correctedStart = career.CurrentDate;
        var revised = store.Move(original,"Revise",original.Terms with
        {
            Start = correctedStart,
            End = StaffOfferRules.LastContractDay(correctedStart,1)
        });
        Assert.Equal(StaffOfferState.Sent,store.Move(revised,"Send").State);
        var history = new SqliteStaffOffers(file,careers.Load(save)).History(draft.Id);
        Assert.Equal(3,history.Count);
        Assert.Equal(start,history[0].Terms.Start);
        Assert.All(history,entry => Assert.Equal(draft.Expires,entry.Expires));
        Assert.Equal(StaffOfferRules.LastContractDay(correctedStart,1),history[^1].Terms.End);
    }

    [Fact]
    public void StaffOfferSeparatesClubDecisionAndPersonalAcceptanceAcrossReload()
    {
        var (careers, file, _) = TacticalCareer();
        var career = careers.Load(new SaveId(Guid.Parse(Path.GetFileNameWithoutExtension(file))));
        var person = SqliteStaffDirectory.Read(file,career).First(p => p.Employment is not null && p.Club != career.ClubId);
        var team = new SqliteClubSquadStore(file).Teams().First(t => t.Category != "First");
        var terms = new StaffOfferTerms(team.Id,"AcademyCoach",career.CurrentDate,career.CurrentDate.AddYears(1).AddDays(-1),
            1_000_000m,"EUR",0,0,[new(StaffClauseKind.RelegationReduction,25,null,null)]);
        var store = new SqliteStaffOffers(file,career);
        Assert.Throws<InvalidDataException>(() => store.Draft(person.Id,terms with { Salary = -1 }));
        var draft = store.Draft(person.Id,terms);
        Assert.Throws<InvalidOperationException>(() => store.Draft(person.Id,terms));
        var sent = store.Move(draft,"Send");
        Assert.Throws<InvalidOperationException>(() => store.Move(draft,"Send"));
        var counter = store.Move(sent,"Evaluate");
        Assert.Equal(StaffOfferState.Counter,counter.State);
        Assert.True(counter.Terms.Compensation > 0);
        var revised = store.Move(counter,"Revise",counter.Terms);
        var agreement = store.Move(store.Move(revised,"Send"),"Evaluate");
        Assert.Equal(StaffOfferState.ClubAgreed,agreement.State);
        var personal = store.Move(agreement,"Personal");
        Assert.Equal(StaffOfferPhase.Personal,personal.Phase);
        var result = store.Move(store.Move(personal,"Send"),"Evaluate");
        Assert.Equal(StaffOfferState.Conditional,result.State);
        Assert.Equal(9,new SqliteStaffOffers(file,career).History(draft.Id).Count);
        Assert.Equal(person.Employment!.Id,SqliteStaffDirectory.Read(file,career).Single(p => p.Id == person.Id).Employment!.Id);
        Assert.Empty(new SqliteTransferStore(file).History());
        Assert.Equal(StaffOfferState.Withdrawn,store.Move(result,"Withdraw").State);
    }

    [Fact]
    public void FreeStaffDecisionUsesReferenceAndContractualRiskWithoutClubNegotiation()
    {
        var date = new DateOnly(2026,7,1);
        var employee = new StaffProfile(Guid.NewGuid(),"Técnico de prueba",null,"AcademyCoach",null,ConfirmedFree:true);
        var buyer = new ClubId(Guid.NewGuid());
        var terms = new StaffOfferTerms(Guid.NewGuid(),"AcademyCoach",date,date.AddYears(1),60000m,"EUR",0,0,
            [new(StaffClauseKind.RelegationReduction,25,null,null)]);
        var sent = new StaffOffer(Guid.NewGuid(),1,employee.Id,buyer,null,StaffOfferPhase.Personal,StaffOfferState.Sent,date,date.AddDays(14),terms);
        Assert.Equal(StaffContractStatus.Free,employee.ContractStatus(date));
        Assert.Equal(StaffOfferState.Rejected,StaffOfferRules.Evaluate(sent with { Terms = terms with { Salary = 30000m } },employee,date,2,2,60000m).State);
        var counter = StaffOfferRules.Evaluate(sent,employee,date,2,2,60000m);
        Assert.Equal(StaffOfferState.Counter,counter.State);
        Assert.True(counter.Terms.Salary > terms.Salary);
        Assert.Equal(StaffOfferState.Conditional,StaffOfferRules.Evaluate(sent with { Terms = counter.Terms },employee,date,2,2,60000m).State);
        Assert.Equal(StaffOfferState.Expired,StaffOfferRules.Evaluate(sent,employee,date.AddDays(15),2,2,60000m).State);
        Assert.Throws<InvalidDataException>(() => StaffOfferRules.Evaluate(sent,employee,date.AddDays(1),2,2,60000m));
    }
}
