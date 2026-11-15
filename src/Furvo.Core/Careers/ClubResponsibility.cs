using Furvo.Core.Domain;

namespace Furvo.Core.Careers;

public enum ManagementAutonomy
{
    Personal,
    Supervised,
    Full
}

public sealed record ClubResponsibility(string Code, string Department);

// Los módulos aportan la aptitud comprobada con sus datos vigentes de personal.
public sealed record ResponsibilityAssignment(Guid? EmployeeId, ManagementAutonomy Autonomy)
{
    public static ResponsibilityAssignment Personal { get; } = new(null, ManagementAutonomy.Personal);

    public void Validate(ClubId club, ClubResponsibility responsibility, Func<ClubId, string, Guid, bool> isEligible)
    {
        if (!Enum.IsDefined(Autonomy) || EmployeeId == Guid.Empty ||
            (EmployeeId is null && Autonomy != ManagementAutonomy.Personal))
            throw new InvalidDataException("La autonomía requiere un responsable válido.");

        if (EmployeeId is { } employee && !isEligible(club, responsibility.Code, employee))
            throw new InvalidDataException("El empleado no está disponible o habilitado para esta responsabilidad.");
    }

    public ResponsibilityAssignment EffectiveFor(ClubId club, ClubResponsibility responsibility,
        Func<ClubId, string, Guid, bool> isEligible) =>
        EmployeeId is { } employee && !isEligible(club, responsibility.Code, employee) ? Personal : this;
}
