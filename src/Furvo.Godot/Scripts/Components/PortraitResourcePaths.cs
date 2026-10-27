using Furvo.Core.Domain;

namespace Furvo.Presentation.Components;

/// <summary>Convención de recursos locales por identidad estable; no acepta rutas proporcionadas por datos o usuarios.</summary>
public static class PortraitResourcePaths
{
    public const string Placeholder = "res://Assets/Portraits/avatar.svg";

    /// <summary>Prepara retratos individuales PNG/WebP sin modificar IDs ni necesitar columnas SQLite.</summary>
    public static IReadOnlyList<string> ForPlayer(PlayerId id)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Se requiere la identidad del futbolista.", nameof(id));
        var file = id.Value.ToString("N");
        return Array.AsReadOnly(new[] { "res://Assets/Portraits/Players/" + file + ".png",
            "res://Assets/Portraits/Players/" + file + ".webp" });
    }
}
