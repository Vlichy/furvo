namespace Furvo.Core.Finance;

public enum MoneySign { NonNegative, Signed }

/// <summary>Importe EUR exacto. El signo se autoriza explícitamente para ajustes o saldos contables.</summary>
public readonly record struct EuroAmount
{
    public long Cents { get; }
    public decimal Euros => Cents / 100m;

    private EuroAmount(long cents) => Cents = cents;

    public static EuroAmount FromCents(long cents, MoneySign sign = MoneySign.NonNegative)
    {
        if (!Enum.IsDefined(sign)) throw new ArgumentOutOfRangeException(nameof(sign));
        if (sign == MoneySign.NonNegative && cents < 0)
            throw new ArgumentOutOfRangeException(nameof(cents), "Este contexto no admite importes negativos.");
        return new EuroAmount(cents);
    }

    public static EuroAmount FromEuros(decimal euros, MoneySign sign = MoneySign.NonNegative)
    {
        // Validar antes del cast: checked detecta desbordamientos, pero no evita el truncamiento.
        if (decimal.Round(euros, 2) != euros)
            throw new InvalidDataException("El importe EUR contiene una fracción de céntimo sin regla de redondeo.");
        return FromCents(checked((long)(euros * 100m)), sign);
    }
}
