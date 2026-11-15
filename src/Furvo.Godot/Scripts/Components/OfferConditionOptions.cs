using Furvo.Core.Market;
namespace Furvo.Presentation.Components;
/// <summary>Opciones de presentación, sin restringir contratos históricos admitidos por Core.</summary>
public static class OfferConditionOptions
{
    public static IReadOnlyList<int> BuybackYears=>TransferOfferPresets.RightYears;
    public static IReadOnlyList<int> MatchingYears {get;}=Array.AsReadOnly(new[]{1,2,3,5});
    public static IReadOnlyList<int> MatchingDays {get;}=Array.AsReadOnly(new[]{3,7,14});
    public static IReadOnlyList<int> ValidityDays {get;}=Array.AsReadOnly(new[]{3,7,14,30});
    public static IReadOnlyList<int> Preserve(IReadOnlyList<int> options,int? previous)=>previous is {} n&&!options.Contains(n)?options.Append(n).ToArray():options;
    public static string YearsKey(int n)=>n==1?"OfferOneYear":"OfferManyYears";
    public static string DaysKey(int n)=>n==1?"OfferOneDay":"OfferManyDays";
}
