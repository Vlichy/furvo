using Furvo.Core.Domain;
namespace Furvo.Core.Careers;
public enum CareerArea { Home,Team,Market,Competition,Club,Career }
public enum CareerPage { Desktop,Inbox,OwnSquad,OwnInformation,OwnEconomy,OwnHistory,PublicClubs,Profile,Contract,NoClub,Lineup,Academy,Training,Analysis,Facilities,MarketSearch,MarketOffers,MarketNegotiations,MarketRights,ClubPersonnel,OwnStadium }
/// <summary>Permisos y ruta propios de una carrera. No usa el club consultado ni escribe fechas/datos.</summary>
public sealed class CareerWorkspace
{
    public CareerSummary Career { get; }
    public CareerPage Page { get; private set; }=CareerPage.Desktop;
    public CareerArea Area=>Page switch{
        CareerPage.Desktop or CareerPage.Inbox=>CareerArea.Home,CareerPage.OwnSquad or CareerPage.Lineup or CareerPage.Academy or CareerPage.Training or CareerPage.Analysis=>CareerArea.Team,
        CareerPage.MarketSearch or CareerPage.MarketOffers or CareerPage.MarketNegotiations or CareerPage.MarketRights=>CareerArea.Market,CareerPage.Profile or CareerPage.Contract=>CareerArea.Career,_=>CareerArea.Club};
    public bool HasClub=>Career.Employment==CareerEmploymentState.Employed && Career.ClubId is not null && Career.Contract is not null;
    public CareerWorkspace(CareerSummary career)
    {
        if(career.Id.Value==Guid.Empty || !Enum.IsDefined(career.Employment) ||
           (career.Employment==CareerEmploymentState.Employed)!=(career.ClubId is not null && career.Contract is not null) ||
           (career.Employment==CareerEmploymentState.Unemployed && (career.ClubId is not null || career.Contract is not null)))
            throw new ArgumentException("Estado laboral de carrera incoherente.");
        if(career.Coach.Id.Value==Guid.Empty || career.SeasonId.Value==Guid.Empty ||
           career.ClubId is {} club && club.Value==Guid.Empty ||
           career.Contract is {} contract && (contract.CoachId!=career.Coach.Id || contract.AcceptedOffer.ClubId!=career.ClubId || contract.AcceptedOffer.SeasonId!=career.SeasonId))
            throw new ArgumentException("Identidades de carrera incompatibles.");
        Career=career;
    }
    public bool CanManageClub(ClubId id)=>HasClub && Career.ClubId==id;
    public void RequireManagement(ClubId id){if(!CanManageClub(id))throw new InvalidOperationException("No dirige este club.");}
    public bool CanOpen(CareerPage page)=>Enum.IsDefined(page) && page!=CareerPage.PublicClubs && (page!=CareerPage.NoClub || !HasClub) && (page is not (CareerPage.OwnSquad or CareerPage.Lineup or CareerPage.Academy or CareerPage.Training or CareerPage.Analysis or CareerPage.Facilities or CareerPage.MarketSearch or CareerPage.MarketOffers or CareerPage.MarketNegotiations or CareerPage.MarketRights or CareerPage.OwnInformation or CareerPage.OwnEconomy or CareerPage.OwnHistory or CareerPage.ClubPersonnel or CareerPage.OwnStadium) || HasClub);
    public void Open(CareerPage page){if(!CanOpen(page))throw new InvalidOperationException("Sección no disponible para esta carrera.");Page=page;}
    public bool CanAdvanceTime=>false; // Pendiente del motor validado de calendario/eventos; no simula un avance.
}
