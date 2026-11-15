using System.Globalization;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;
/// <summary>Solo recibe la proyección de información conocida. No puede recuperar términos privados.</summary>
public static class ReleaseClauseText
{
    public static string Value(ReleaseClauseInfo? clause,Func<string,string> text)
    {
        if(clause is null||clause.State==ReleaseInformationState.Unknown)return text("ReleaseUnknown");
        if(clause.State==ReleaseInformationState.Absent)return text("ReleaseAbsent");
        if(clause.State==ReleaseInformationState.NotApplicable)return text("ReleaseNotApplicable");
        var amount=clause.Amount?.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" "+clause.Currency;
        return amount+(clause.Applicability==ReleaseApplicability.PendingConditions?" · "+text("ReleasePendingConditions"):"");
    }
    public static string Notice(ReleaseClauseInfo? clause,decimal amount,string currency,Func<string,string> text)
    {
        var key="ReleaseNotice_"+ContractInformation.Notice(clause,amount,currency);
        return string.Format(CultureInfo.GetCultureInfo("es-ES"),text(key),Value(clause,text));
    }
}
