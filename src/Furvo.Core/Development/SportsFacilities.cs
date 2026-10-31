using Furvo.Core.Domain;
namespace Furvo.Core.Development;
public enum FacilityOwnership { Unknown=0,Absent=1,Installed=2,Ordered=3 }
public sealed record SportsFacility(Guid Id,ClubId Club,string Technology,FacilityOwnership Ownership,int Capacity,DateOnly? AvailableFrom,string? Evidence)
{
    public bool Available(DateOnly date)=>Ownership==FacilityOwnership.Installed&&AvailableFrom is {} start&&date>=start&&!string.IsNullOrWhiteSpace(Evidence);
    public void Validate(){if(Id==Guid.Empty||Club.Value==Guid.Empty||Technology!="footbonaut"||!Enum.IsDefined(Ownership)||Capacity!=1||Ownership==FacilityOwnership.Installed&&(AvailableFrom is null||string.IsNullOrWhiteSpace(Evidence)))throw new InvalidDataException("Instalación sin verificación o capacidad inválida.");}
}
public sealed record FacilityAcquisition(Guid Id,ClubId Club,string Technology,DateOnly RequestedAt,decimal? Purchase,decimal? Installation,decimal? AnnualMaintenance,decimal? Operating,string? Currency,int? LeadDays,bool? SpaceConfirmed,string Status,string? Authorization);
public interface IFacilityPurchaseAuthorization
{bool CanCommit(FacilityAcquisition request,decimal cash,string currency);}
