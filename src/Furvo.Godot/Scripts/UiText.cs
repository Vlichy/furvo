using System.Globalization;
using System.Resources;
using Furvo.Core.Domain;

namespace Furvo.Presentation;

/// <summary>Único punto de resolución de textos; recursos neutros en español y preparado para futuros satélites de idioma.</summary>
internal static class UiText
{
    private static readonly ResourceManager Resources = new("Furvo.Godot.Resources.UiStrings", typeof(UiText).Assembly);
    public static string Get(string key) => Resources.GetString(key, CultureInfo.GetCultureInfo("es")) ?? key;
    public static string Format(string key, params object?[] values) =>
        string.Format(CultureInfo.GetCultureInfo("es-ES"), Get(key), values);
    public static string Position(PlayerRole? role, PlayerPosition? group) => role is { } detailed
        ? Get("Position_" + detailed) : group is { } broad ? Get("Group_" + broad) : Get("Unknown");
}
