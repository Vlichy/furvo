using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Core.Careers;

public readonly record struct ManagerContractId
{
    public Guid Value { get; }
    public ManagerContractId(Guid value) { if(value==Guid.Empty)throw new ArgumentException("Identidad vacía."); Value=value; }
    public static ManagerContractId New()=>new(Guid.NewGuid());
    public override string ToString()=>Value.ToString("D");
}
public enum CareerStartMode { Unemployed=1, RejectOffer=2, SignOffer=3 }
public enum CareerEmploymentState { Unemployed=1, Employed=2 }
public sealed record CareerCoach(CoachId Id,CoachProfile ProfileSnapshot);
public sealed record ManagerContract(ManagerContractId Id,CoachId CoachId,ManagerOffer AcceptedOffer,int SnapshotVersion=2);
public sealed record CareerCreationRequest(SaveId Id,CoachId CoachId,CoachProfile ProfileSnapshot,CareerStartMode Mode,ManagerOffer? Offer,ManagerContractId? ContractId);
public sealed record CareerSummary(SaveId Id,WorldId WorldId,SeasonId SeasonId,DateOnly CurrentDate,CareerCoach Coach,
    CareerEmploymentState Employment,ClubId? ClubId,ManagerContract? Contract,ManagerOffer? RejectedOffer);
public interface ICareerStore
{
    CareerSummary Create(CareerCreationRequest request);
    CareerSummary Load(SaveId id);
}

/// <summary>Estado temporal de nueva carrera; elegir club o volver no persiste nada. Confirmar mantiene el token para reintentos.</summary>
public sealed class CareerCreationFlow
{
    private readonly IWorldQueries _queries; private readonly IManagerOfferSource _offers; private readonly ICareerStore _careers;
    public CoachProfile? Profile {get;private set;}
    public ManagerOffer? Offer {get;private set;}
    public CareerCreationRequest? Pending {get;private set;}
    public CareerCreationFlow(IWorldQueries queries,IManagerOfferSource offers,ICareerStore careers) { _queries=queries;_offers=offers;_careers=careers; }
    public void SelectProfile(CoachProfile profile) { profile.ValidateAge(_queries.ReferenceDate); Profile=profile;Offer=null;Pending=null; }
    public ManagerOffer SelectClub(ClubId id)
    {
        if(Profile is null)throw new InvalidOperationException("Selecciona un entrenador.");
        _=_queries.GetClubDetails(id); Offer=_offers.Create(Profile,id).Freeze();Pending=null;
        if(Offer.ProfileId!=Profile.Id || Offer.ClubId!=id)throw new InvalidOperationException("Oferta de otra identidad.");
        return Offer;
    }
    public void BackToClubs() { Offer=null;Pending=null; }
    public CareerCreationRequest Prepare(CareerStartMode mode)
    {
        if(Profile is null || !Enum.IsDefined(mode))throw new InvalidOperationException("Inicio no válido.");
        if(mode!=CareerStartMode.Unemployed && Offer is null)throw new InvalidOperationException("Falta la oferta.");
        if(mode==CareerStartMode.SignOffer && Offer!.SigningBlocks().Count>0)throw new InvalidOperationException("La oferta no admite firma.");
        if(Pending is { } old && old.Mode==mode)return old;
        Pending=new CareerCreationRequest(SaveId.New(),CoachId.New(),Profile,mode,mode==CareerStartMode.Unemployed?null:Offer!.Freeze(),mode==CareerStartMode.SignOffer?ManagerContractId.New():null);
        return Pending;
    }
    public CareerSummary Confirm()=>_careers.Create(Pending??throw new InvalidOperationException("Confirma primero las condiciones de inicio."));
    public void CancelConfirmation()=>Pending=null;
}
