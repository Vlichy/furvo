using System.Text.Json;
using System.Text.Json.Serialization;
using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Presentation.Components;

/// <summary>Vista previa editorial separada del catálogo deportivo. No convierte propuestas en políticas vigentes.</summary>
public static class ClubDirectionProposalCatalog
{
    private sealed record Document(string Version, string Status, string Language, Row[] Clubs);
    private sealed record Row(string ClubId, ClubPrincipleProposal[] Principles, BoardObjectiveProposal[] Objectives);
    private static readonly Lazy<IReadOnlyDictionary<string, ClubDirectionProposal>> Catalog = new(() =>
    {
        using var stream = typeof(ClubDirectionProposalCatalog).Assembly.GetManifestResourceStream("Furvo.ClubDirectionProposals")
            ?? throw new InvalidDataException("Falta el recurso de propuestas institucionales.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    });
    /// <summary>La aprobación futura requiere otro flujo explícito; un cambio de bandera no publica objetivos.</summary>
    public static IReadOnlyDictionary<string, ClubDirectionProposal> Parse(string json)
    {
        var options = new JsonSerializerOptions(); options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        var document = JsonSerializer.Deserialize<Document>(json, options)
            ?? throw new InvalidDataException("Documento editorial vacío.");
        if (document.Status != "Draft" || document.Language != "es" || string.IsNullOrWhiteSpace(document.Version) || document.Clubs is null)
            throw new InvalidDataException("Solo se admiten borradores editoriales en español.");
        var items = new Dictionary<string, ClubDirectionProposal>(StringComparer.Ordinal);
        foreach (var row in document.Clubs)
        {
            if (row is null || !Guid.TryParseExact(row.ClubId, "D", out var id) || id == Guid.Empty || row.Principles is null || row.Principles.Length is < 3 or > 5 || row.Objectives is null || row.Objectives.Length == 0)
                throw new InvalidDataException("Club o cantidad de principios no válidos.");
            var codes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in row.Principles)
                if (p is null || !ValidText(p.Code, p.Title, p.Description) || !codes.Add(p.Code))
                    throw new InvalidDataException("Principio editorial incompleto o repetido.");
            codes.Clear();
            foreach (var objective in row.Objectives)
                if (objective is null || !ValidText(objective.Code, objective.Title, objective.Description) || !codes.Add(objective.Code) ||
                    !Enum.IsDefined(objective.Priority) || !Enum.IsDefined(objective.Horizon) || objective.StartSeason is < 1 or > 9998 ||
                    objective.DueSeason < objective.StartSeason || objective.DueSeason > 9998 ||
                    (objective.Horizon == BoardObjectiveHorizon.Season && objective.DueSeason != objective.StartSeason) ||
                    (objective.Horizon != BoardObjectiveHorizon.Season && objective.DueSeason <= objective.StartSeason))
                    throw new InvalidDataException("Objetivo editorial o plazo no válido.");
            if (!Enum.GetValues<BoardObjectiveHorizon>().All(h => row.Objectives.Any(o => o.Horizon == h)))
                throw new InvalidDataException("Falta un horizonte de objetivos.");
            var first = row.Objectives[0].StartSeason;
            if (row.Objectives.Any(o => o.StartSeason != first) || row.Objectives.Where(o => o.Horizon == BoardObjectiveHorizon.MediumTerm).Max(o => o.DueSeason) >=
                row.Objectives.Where(o => o.Horizon == BoardObjectiveHorizon.LongTerm).Min(o => o.DueSeason))
                throw new InvalidDataException("Horizontes editoriales incoherentes.");
            if (!items.TryAdd(row.ClubId, new ClubDirectionProposal(new ClubId(id), document.Version,
                Array.AsReadOnly(row.Principles), Array.AsReadOnly(row.Objectives))))
                throw new InvalidDataException("Club repetido en las propuestas.");
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, ClubDirectionProposal>(items);
    }
    private static bool ValidText(params string[] values) => values.All(v => !string.IsNullOrWhiteSpace(v));
    public static ClubDirectionProposal? Find(string clubId) => Catalog.Value.GetValueOrDefault(clubId);
}
