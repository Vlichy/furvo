namespace Furvo.Presentation.Components;

/// <summary>Marcas de referencia en metros: campo horizontal, sin áreas en las bandas. No atribuye medidas a ningún estadio.</summary>
public readonly record struct PitchBox(double X, double Y, double Width, double Height);
public static class FootballPitchGeometry
{
    public const double Length = 105, Width = 68, PenaltyRadius = 9.15, PenaltySpot = 11, GoalWidth = 7.32;
    public static IReadOnlyList<PitchBox> EndAreas { get; } = Array.AsReadOnly(new[]
    {
        new PitchBox(0, (Width - 40.32)/2, 16.5, 40.32), new PitchBox(Length - 16.5, (Width - 40.32)/2, 16.5, 40.32),
        new PitchBox(0, (Width - 18.32)/2, 5.5, 18.32), new PitchBox(Length - 5.5, (Width - 18.32)/2, 5.5, 18.32)
    });
    public static double PenaltyArcHalfAngle => Math.Acos((16.5 - PenaltySpot) / PenaltyRadius);
}
