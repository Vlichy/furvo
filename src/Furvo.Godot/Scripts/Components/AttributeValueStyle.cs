namespace Furvo.Presentation.Components;

/// <summary>Umbrales y colores exclusivos de presentación; no transforma puntuaciones ni expresa rendimiento deportivo.</summary>
public static class AttributeValueStyle
{
    public const int LowMaximum = 5;
    public const int NeutralMaximum = 10;
    public const int GoodMaximum = 15;
    public const string LowColor = "d99391";
    public const string NeutralColor = "dce5f0";
    public const string GoodColor = "a8cda7";
    public const string HighColor = "69d7a0";

    /// <summary>El número sigue mostrándose siempre; el color es una ayuda adicional de lectura.</summary>
    public static string ColorHex(int value)
    {
        if (value is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(value));
        return value <= LowMaximum ? LowColor : value <= NeutralMaximum ? NeutralColor :
            value <= GoodMaximum ? GoodColor : HighColor;
    }
}
