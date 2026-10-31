using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;
public sealed record PositionInsights(IReadOnlyList<VisibleAttribute> Strengths,IReadOnlyList<VisibleAttribute> Weaknesses);
/// <summary>Adaptador compatible: ficha e informes comparten el mismo catálogo descriptivo de Core.</summary>
public static class PositionInsightRules
{
    public const int StrengthMinimum=PositionInsightCatalog.StrengthMinimum;
    public const int WeaknessMaximum=PositionInsightCatalog.WeaknessMaximum;
    public const int MaximumItems=PositionInsightCatalog.MaximumItems;
    public static IReadOnlyList<string> RelevantFor(PlayerRole role)=>PositionInsightCatalog.RelevantFor(role);
    public static PositionInsights Describe(PlayerRole? role,IEnumerable<VisibleAttributeGroup> groups){var value=PositionInsightCatalog.Describe(role,groups);return new(value.Strengths,value.Weaknesses);}
}
