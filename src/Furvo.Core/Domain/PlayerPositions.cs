namespace Furvo.Core.Domain;

/// <summary>Desglose compatible de posición; no representa una función táctica ni una ponderación deportiva.</summary>
public enum PlayerRole
{
    Goalkeeper = 1, CentreBack = 2, RightBack = 3, LeftBack = 4,
    DefensiveMidfielder = 5, CentralMidfielder = 6, AttackingMidfielder = 7,
    RightMidfielder = 8, LeftMidfielder = 9, Striker = 10,
    RightWinger = 11, LeftWinger = 12, SecondStriker = 13
}

/// <summary>Posición principal detallada y secundarias distintas, conservando los cuatro grupos originales.</summary>
public sealed class PlayerPositions
{
    public PlayerRole Primary { get; }
    public IReadOnlyList<PlayerRole> Secondary { get; }

    /// <summary>Copia las posiciones; un portero no recibe posiciones de campo y ninguna secundaria duplica la principal.</summary>
    public PlayerPositions(PlayerRole primary, IEnumerable<PlayerRole>? secondary = null)
    {
        _ = GroupOf(primary);
        var values = (secondary ?? []).ToArray();
        foreach (var role in values) _ = GroupOf(role);
        if (values.Contains(primary) || values.Distinct().Count() != values.Length)
            throw new ArgumentException("Posiciones secundarias repetidas o iguales a la principal.", nameof(secondary));
        if ((primary == PlayerRole.Goalkeeper && values.Length != 0) || values.Contains(PlayerRole.Goalkeeper))
            throw new ArgumentException("La portería no se mezcla con posiciones de campo.", nameof(secondary));
        Primary = primary;
        Secondary = Array.AsReadOnly(values.OrderBy(x => (int)x).ToArray());
    }

    /// <summary>Traduce el desglose a los grupos 1–4 existentes sin cambiar sus valores persistidos.</summary>
    public static PlayerPosition GroupOf(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => PlayerPosition.Goalkeeper,
        PlayerRole.CentreBack or PlayerRole.RightBack or PlayerRole.LeftBack => PlayerPosition.Defender,
        PlayerRole.DefensiveMidfielder or PlayerRole.CentralMidfielder or PlayerRole.AttackingMidfielder
            or PlayerRole.RightMidfielder or PlayerRole.LeftMidfielder => PlayerPosition.Midfielder,
        PlayerRole.Striker or PlayerRole.RightWinger or PlayerRole.LeftWinger or PlayerRole.SecondStriker => PlayerPosition.Forward,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
