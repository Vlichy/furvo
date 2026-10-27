using System.Text.Json;

namespace Furvo.Presentation.Components;

public sealed record ClubKitVisual(string Primary, string Secondary, int Pattern);
public sealed record ClubVisual(string ClubId, string Monogram, int EmblemMotif, ClubKitVisual[] Kits);

/// <summary>Recurso editorial de presentación por identidad estable. No contiene datos deportivos ni diseños definitivos.</summary>
public static class ClubVisualCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, ClubVisual>> Items = new(() =>
    {
        using var stream = typeof(ClubVisualCatalog).Assembly.GetManifestResourceStream("Furvo.ClubVisuals")
            ?? throw new InvalidDataException("Falta el recurso de identidad visual provisional.");
        var rows = JsonSerializer.Deserialize<ClubVisual[]>(stream) ?? [];
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.ClubId, out _) || string.IsNullOrWhiteSpace(row.Monogram) || row.Kits.Length != 3 || row.EmblemMotif is < 0 or > 3)
                throw new InvalidDataException("Identidad visual provisional no válida.");
            foreach (var kit in row.Kits)
                if (!IsColor(kit.Primary) || !IsColor(kit.Secondary) || kit.Pattern is < 0 or > 2)
                    throw new InvalidDataException("Equipación provisional no válida.");
        }
        return rows.ToDictionary(r => r.ClubId, StringComparer.Ordinal);
    });
    private static bool IsColor(string value) => value.Length == 6 && value.All(Uri.IsHexDigit);
    // Un club futuro no adopta por error la identidad de otro club publicado.
    public static ClubVisual Get(string clubId) => Items.Value.TryGetValue(clubId, out var item) ? item :
        new(clubId, "?", 0, [new("40536b", "a6b6ca", 0), new("a6b6ca", "40536b", 1), new("263d55", "a6b6ca", 2)]);
}
