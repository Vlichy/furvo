using Furvo.Core.Careers;

namespace Furvo.Core.Development;

public sealed record TrainingStaffContribution(Guid Employee, string Attribute, int Value, decimal Factor);

public static class TrainingStaffEffect
{
    public const string Version = "training-staff-v1";

    public static TrainingStaffContribution? Resolve(TrainingPlan plan, TrainingSession session,
        IReadOnlyList<TrainingStaff> staff, bool youth, Func<Guid, StaffProfession?> professional)
    {
        var exercise = TrainingCatalog.Get(session.Exercise);
        if (exercise.Area is TrainingArea.Recovery or TrainingArea.Technology) return null;
        var area = TrainingRules.Responsibility(session);
        var assignment = plan.Delegations.SingleOrDefault(item => item.Area == area);
        // La delegación completa aporta el responsable salvo que la sesión indique otro.
        // En supervisada o personal con apoyo se exige el responsable explícito del bloque.
        var employee = session.Responsible ?? (assignment?.Mode == TrainingDelegationMode.Delegated ? assignment.Responsible : null);
        if (employee is not { } id || assignment?.Responsible is { } assigned && assigned != id ||
            !staff.Any(s => s.Id == id && s.Team == session.Team && s.Responsibilities.Contains(area)))
            return null;
        var profile = professional(id);
        if (profile is null) return null;
        var key = exercise.Area switch
        {
            TrainingArea.Gym => "Fitness",
            TrainingArea.SetPieces => "Tactics",
            TrainingArea.Goalkeepers => "KeeperTechnique",
            _ => session.Exercise switch
            {
                "technical" or "passing" when youth => "YouthDevelopment",
                "technical" or "passing" => "Technical",
                "finishing" => "Attacking",
                "defending" => "Defending",
                "collective" => "Tactics",
                _ => ""
            }
        };
        if (!profile.Attributes.TryGetValue(key, out var value) || value is < 1 or > 20) return null;
        var factor = Math.Clamp(1m + (value - 11m) / 100m, .95m, 1.05m);
        return new(id, key, value, factor);
    }
}
