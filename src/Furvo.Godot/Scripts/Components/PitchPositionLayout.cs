using Furvo.Core.Domain;

namespace Furvo.Presentation.Components;

/// <summary>Marca normalizada de posición: el ataque avanza de izquierda a derecha, sin selección táctica.</summary>
public sealed record PitchPositionMarker(PlayerRole Role, bool IsPrimary, double X, double Y);

/// <summary>Coordenadas visuales reutilizables; no deduce posiciones, capacidad ni asignaciones de alineación.</summary>
public static class PitchPositionLayout
{
    /// <summary>Solo devuelve la principal y las secundarias recibidas, en orden estable, sin inventar posiciones.</summary>
    public static IReadOnlyList<PitchPositionMarker> Create(PlayerRole? primary, IEnumerable<PlayerRole>? secondary = null)
    {
        var others = (secondary ?? []).ToArray();
        if (primary is null)
        {
            if (others.Length != 0) throw new ArgumentException("Las secundarias requieren una posición principal.", nameof(secondary));
            return Array.Empty<PitchPositionMarker>();
        }
        // Reutiliza las validaciones de posición ya aprobadas, incluyendo incompatibilidad portero/campo y duplicados.
        var positions = new PlayerPositions(primary.Value, others);
        var markers = new List<PitchPositionMarker> { Marker(primary.Value, true) };
        markers.AddRange(positions.Secondary.Select(role => Marker(role, false)));
        return markers.AsReadOnly();
    }

    private static PitchPositionMarker Marker(PlayerRole role, bool primary)
    {
        var point = role switch
        {
            PlayerRole.Goalkeeper => (0.08, 0.50),
            PlayerRole.CentreBack => (0.24, 0.50),
            PlayerRole.RightBack => (0.24, 0.82),
            PlayerRole.LeftBack => (0.24, 0.18),
            PlayerRole.DefensiveMidfielder => (0.40, 0.50),
            PlayerRole.CentralMidfielder => (0.53, 0.50),
            PlayerRole.AttackingMidfielder => (0.66, 0.50),
            PlayerRole.RightMidfielder => (0.56, 0.82),
            PlayerRole.LeftMidfielder => (0.56, 0.18),
            PlayerRole.Striker => (0.90, 0.50),
            PlayerRole.RightWinger => (0.79, 0.82),
            PlayerRole.LeftWinger => (0.79, 0.18),
            PlayerRole.SecondStriker => (0.78, 0.50),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        return new PitchPositionMarker(role, primary, point.Item1, point.Item2);
    }
}
