using Furvo.Core.Domain;
namespace Furvo.Core.Development;
public enum AnalysisReportKind { Technical=1,SquadSummary=2,Individual=3 }
public enum AnalysisScope { Collective=1,Individual=2,Position=3,Group=4 }
public enum AnalysisReportState { Complete=1 }
public sealed record AnalysisSelection(AnalysisScope Scope=AnalysisScope.Collective,PlayerId? Player=null,PlayerRole? Position=null,bool IncludeSecondary=false,Guid? Group=null);
public sealed record AnalysisStaff(Guid Id,string Name,string Role);
/// <summary>Instantánea pública: no contiene atributos ocultos ni acumuladores internos de desarrollo.</summary>
public sealed record AnalysisPlayerSnapshot(Furvo.Core.Queries.PlayerDetails Details,int ScheduledSessions,decimal PlannedLoad,decimal? ObservedFatigue,DateOnly? PhysicalObservedAt,
    IReadOnlyList<Furvo.Core.Queries.VisibleAttribute> Strengths,IReadOnlyList<Furvo.Core.Queries.VisibleAttribute> Weaknesses,IReadOnlyDictionary<string,int> ObservedAttributeChanges);
public sealed record AnalysisReportContext(Guid Career,AnalysisSelection Selection,AnalysisStaff Staff,string TeamName,string? GroupName,IReadOnlyList<AnalysisPlayerSnapshot> Players,int FormatVersion=2,AnalysisReportState State=AnalysisReportState.Complete);
public sealed record AnalysisFinding(string Topic,string Content,string Source,DateOnly ObservedAt);
public sealed record AnalysisReport(Guid Id,Guid Team,AnalysisReportKind Kind,PlayerId? Player,ClubId? Opponent,DateOnly Date,Guid? Analyst,IReadOnlyList<AnalysisFinding> Findings,IReadOnlyList<string> Priorities,AnalysisReportContext? Context=null)
{
    public void Validate(IReadOnlySet<PlayerId> squad,IReadOnlySet<Guid> staff)
    {if(Context is not null)AnalysisReportRules.ValidateSnapshot(this);if(Id==Guid.Empty||Team==Guid.Empty||!Enum.IsDefined(Kind)||Date==default||Context is null&&Kind==AnalysisReportKind.Individual&&(Player is not {} id||!squad.Contains(id))||Analyst is {} a&&!staff.Contains(a)||Findings.Any(f=>string.IsNullOrWhiteSpace(f.Topic)||string.IsNullOrWhiteSpace(f.Content)||string.IsNullOrWhiteSpace(f.Source)||f.ObservedAt>Date)||Priorities.Distinct().Count()!=Priorities.Count||Priorities.Any(p=>!Findings.Any(f=>f.Topic==p)))throw new InvalidDataException("Informe sin datos verificables, jugador o responsable válido.");}
}
public static class AnalysisReportRules
{
    public static IReadOnlyList<Furvo.Core.Queries.PlayerDetails> Select(AnalysisSelection selection,IReadOnlyList<Furvo.Core.Queries.PlayerDetails> squad,IReadOnlyList<TrainingGroup> groups)
    {
        if(!Enum.IsDefined(selection.Scope))throw new InvalidDataException("Ámbito desconocido.");
        var matching=selection.Scope switch{
            AnalysisScope.Collective when selection.Player is null&&selection.Position is null&&selection.Group is null=>squad,
            AnalysisScope.Individual when selection.Player is not null&&selection.Position is null&&selection.Group is null=>squad.Where(p=>p.Player.Id==selection.Player).ToArray(),
            AnalysisScope.Position when selection.Player is null&&selection.Position is {} position&&Enum.IsDefined(position)&&selection.Group is null=>squad.Where(p=>p.Player.Position==position||selection.IncludeSecondary&&p.SecondaryPositions.Contains(position)).ToArray(),
            AnalysisScope.Group when selection.Player is null&&selection.Position is null&&selection.Group is {} id=>Group(id,squad,groups),
            _=>throw new InvalidDataException("La selección no corresponde al ámbito del informe.")};
        if(matching.Count==0||matching.Select(p=>p.Player.Id).Distinct().Count()!=matching.Count)throw new InvalidDataException("El ámbito debe incluir al menos un futbolista elegible, sin duplicados.");return matching.OrderBy(p=>p.Player.Id.ToString(),StringComparer.Ordinal).ToArray();
    }
    private static IReadOnlyList<Furvo.Core.Queries.PlayerDetails> Group(Guid id,IReadOnlyList<Furvo.Core.Queries.PlayerDetails> squad,IReadOnlyList<TrainingGroup> groups)
    {var g=groups.SingleOrDefault(g=>g.Id==id)??throw new InvalidDataException("Selecciona un grupo existente.");if(g.Members.Count==0||g.Members.Distinct().Count()!=g.Members.Count||g.Members.Any(id=>squad.All(p=>p.Player.Id!=id)))throw new InvalidDataException("El grupo contiene miembros que ya no pertenecen a esta plantilla; revisa su composición.");return squad.Where(p=>g.Members.Contains(p.Player.Id)).ToArray();}
    public static void ValidateSnapshot(AnalysisReport report)
    {
        var context=report.Context??throw new InvalidDataException("Los informes nuevos requieren ámbito, integrantes y técnico asignado.");
        if(context.Career==Guid.Empty||context.FormatVersion!=2||context.State!=AnalysisReportState.Complete||context.Staff.Id==Guid.Empty||report.Analyst!=context.Staff.Id||string.IsNullOrWhiteSpace(context.Staff.Name)||context.Players.Count==0||context.Players.Select(p=>p.Details.Player.Id).Distinct().Count()!=context.Players.Count)throw new InvalidDataException("Instantánea de informe incompleta.");
        if(!Enum.IsDefined(context.Selection.Scope)||context.Players.Any(p=>p.ScheduledSessions<0||p.PlannedLoad<0||p.ObservedFatigue is <0 or >100)||context.Selection.Scope!=AnalysisScope.Individual&&report.Player is not null)throw new InvalidDataException("Ámbito o medidas del informe inválidos.");
        if(context.Selection.Scope==AnalysisScope.Individual&&(context.Players.Count!=1||context.Selection.Player!=context.Players[0].Details.Player.Id||report.Player!=context.Selection.Player))throw new InvalidDataException("El informe individual no corresponde al jugador seleccionado.");
        foreach(var player in context.Players)if(player.Details.AttributeGroups.SelectMany(g=>g.Values).Select(a=>a.Code).Distinct().Count()!=51||player.Strengths.Concat(player.Weaknesses).Any(a=>PlayerAttributeCatalog.Get(a.Code).IsHidden||a.Value is <1 or >20)||player.Details.AttributeGroups.Any(g=>g.Category==AttributeCategory.Hidden)||player.Details.AttributeGroups.SelectMany(g=>g.Values).Any(a=>PlayerAttributeCatalog.Get(a.Code).IsHidden||a.Value is <1 or >20)||player.ObservedAttributeChanges.Keys.Any(k=>PlayerAttributeCatalog.Get(k).IsHidden))throw new InvalidDataException("Un informe público no puede contener atributos ocultos.");
    }
}
public sealed record AnalysisRequest(Guid Id,Guid Team,string Subject,DateOnly Date,Guid? Responsible,string Status);
