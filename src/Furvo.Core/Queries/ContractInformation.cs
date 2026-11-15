using Furvo.Core.Domain;
namespace Furvo.Core.Queries;
public enum ReleaseInformationState { Unknown=0,Absent=1,Known=2,NotApplicable=3 }
public enum ReleaseApplicability { Unknown=0,Applicable=1,PendingConditions=2,NotApplicable=3 }
public enum ReleaseInformationSource { Unknown=0,OwnContract=1,PublicConfirmed=2 }
/// <summary>Proyección segura: jamás lleva el contrato interno ni su evidencia/confidencialidad.</summary>
public sealed record ReleaseClauseInfo(ReleaseInformationState State,decimal? Amount=null,string? Currency=null,ReleaseApplicability Applicability=ReleaseApplicability.Unknown,ReleaseInformationSource Source=ReleaseInformationSource.Unknown)
{public static ReleaseClauseInfo Unknown {get;}=new(ReleaseInformationState.Unknown);}
public sealed record PlayerContractInfo(ContractId? Id,string? Club,DateOnly? End,ReleaseClauseInfo Release);
public sealed record ReleaseClauseContext(DateOnly Date,ClubId? ViewerClub,string? OwnerCountry=null,string? BuyerCountry=null,Guid? Competition=null,ClubId? BuyerClub=null);
public enum ReleaseOfferNotice { Unknown,Absent,NotApplicable,PendingConditions,CurrencyMismatch,Below,AtOrAbove }
public static class ContractInformation
{
    public static ReleaseClauseInfo Release(Contract? contract,ReleaseClauseContext context)
    {
        var clause=contract?.ReleaseClause;if(contract is null||clause is null||clause.Presence==ReleaseClausePresence.Unknown||clause.ConfirmedOn is {} confirmed&&confirmed>context.Date)return ReleaseClauseInfo.Unknown;
        var own=context.ViewerClub==contract.ClubId;var known=own||clause.Disclosure==ReleaseClauseDisclosure.PublicConfirmed&&!string.IsNullOrWhiteSpace(clause.Evidence)&&clause.ConfirmedOn<=context.Date;
        if(!known)return ReleaseClauseInfo.Unknown;var source=own?ReleaseInformationSource.OwnContract:ReleaseInformationSource.PublicConfirmed;
        if(contract.StartDate is {} start&&context.Date<start||contract.EndDate is {} end&&context.Date>end){
            if(clause.Presence==ReleaseClausePresence.Absent)return ReleaseClauseInfo.Unknown;
            return new(ReleaseInformationState.NotApplicable,Applicability:ReleaseApplicability.NotApplicable,Source:source);
        }
        if(clause.From is {} from&&context.Date<from||clause.Until is {} until&&context.Date>until)
            return new(ReleaseInformationState.NotApplicable,Applicability:ReleaseApplicability.NotApplicable,Source:source);
        if(clause.Presence==ReleaseClausePresence.Absent)return new(ReleaseInformationState.Absent,Source:source);
        var pending=contract.StartDate is null||contract.EndDate is null;
        if(clause.Scope!=ReleaseClauseScope.AnyClub){if(context.OwnerCountry is null||context.BuyerCountry is null)pending=true;else if((clause.Scope==ReleaseClauseScope.Domestic)!=(context.OwnerCountry==context.BuyerCountry))return new(ReleaseInformationState.NotApplicable,Applicability:ReleaseApplicability.NotApplicable,Source:source);}
        foreach(var condition in clause.Conditions??Array.Empty<ReleaseClauseCondition>()){
            var actual=condition.Kind==ReleaseConditionKind.BuyerClub?context.BuyerClub?.Value:context.Competition;
            if(actual is null)pending=true;else if(actual!=condition.Reference)return new(ReleaseInformationState.NotApplicable,Applicability:ReleaseApplicability.NotApplicable,Source:source);
        }
        return new(ReleaseInformationState.Known,clause.Amount,clause.Currency,pending?ReleaseApplicability.PendingConditions:ReleaseApplicability.Applicable,source);
    }
    public static ReleaseOfferNotice Notice(ReleaseClauseInfo? clause,decimal amount,string currency)
    {
        if(clause is null||clause.State==ReleaseInformationState.Unknown)return ReleaseOfferNotice.Unknown;
        if(clause.State==ReleaseInformationState.Absent)return ReleaseOfferNotice.Absent;
        if(clause.State==ReleaseInformationState.NotApplicable)return ReleaseOfferNotice.NotApplicable;
        if(clause.Amount is null||clause.Currency is null)return ReleaseOfferNotice.Unknown;
        if(clause.Applicability!=ReleaseApplicability.Applicable)return ReleaseOfferNotice.PendingConditions;
        if(clause.Currency!=currency)return ReleaseOfferNotice.CurrencyMismatch;
        return amount<clause.Amount?ReleaseOfferNotice.Below:ReleaseOfferNotice.AtOrAbove;
    }
}
