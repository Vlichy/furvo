using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

public sealed partial class CareerCreationTests
{
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
    }
}
