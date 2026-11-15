#if DEBUG
using Furvo.Core.Domain;
using Furvo.Core.Careers;
using Furvo.Persistence;
namespace Furvo.Presentation;
public partial class Main
{
    /// <summary>Fixtures exclusivamente de QA en almacenamiento configurado antes del árbol. No existe en Release ni en la UI.</summary>
    public void PrepareReleaseInformationForValidation(string player,int mode,bool offer,bool own=false)
    {
        if(_careerDataRoot is null||_workspace is null)throw new InvalidOperationException("Se requiere una carrera de prueba aislada.");
        var data=Transfers().Participants();var id=own?data.Players.First(p=>p.Category=="First"&&p.Details.Club?.Id==data.ManagedClub).Details.Player.Id:new PlayerId(Guid.Parse(player));
        if(mode==6){var incomplete=data.Players.First(p=>p.Contract is null&&p.Details.Club is not null&&p.Details.Club.Id!=data.ManagedClub);if(offer)ShowTransferForm(incomplete.Details.Player.Id);else ShowMarketPlayer(incomplete.Details.Player.Id);return;}
        var world=new SqliteWorldStore(SportsPath).Load();var contract=world.Contracts.Single(c=>c.PlayerId==id&&c.StartDate<=data.Date&&c.EndDate>=data.Date);
        ContractReleaseClause clause=mode switch{
            0=>new(contract.Id,ReleaseClausePresence.Unknown),
            1=>new(contract.Id,ReleaseClausePresence.Absent,Disclosure:ReleaseClauseDisclosure.PublicConfirmed,Evidence:"QA fixture: ausencia confirmada",ConfirmedOn:data.Date),
            2 or 3 or 4 or 5=>new(contract.Id,ReleaseClausePresence.Present,12_000_000,"EUR",From:mode==4?data.Date.AddDays(1):null,
                Conditions:mode==5?new[]{new ReleaseClauseCondition(ReleaseConditionKind.Competition,world.Competitions[0].Id.Value)}:null,
                Disclosure:mode==3?ReleaseClauseDisclosure.ClubOnly:ReleaseClauseDisclosure.PublicConfirmed,Evidence:"QA fixture: información sintética",ConfirmedOn:data.Date),
            _=>throw new ArgumentOutOfRangeException(nameof(mode))};
        new SqliteContractReleaseStore(SportsPath).Record(clause);
        if(own){NavigateCareer(CareerPage.OwnSquad);ShowPlayer(id);}else if(offer)ShowTransferForm(id);else ShowMarketPlayer(id);
    }
}
#endif
