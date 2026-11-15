using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Xunit;

namespace Furvo.Tests;

public sealed class ClubResponsibilityTests
{
    [Fact]
    public void AutonomyWithoutEmployeeIsRejected()
    {
        var assignment = new ResponsibilityAssignment(null, ManagementAutonomy.Full);
        Assert.Throws<InvalidDataException>(() => assignment.Validate(
            new ClubId(Guid.NewGuid()), new("training", "first-team"), (_, _, _) => true));
    }

    [Fact]
    public void UnavailableEmployeeReturnsControlToManager()
    {
        var assignment = new ResponsibilityAssignment(Guid.NewGuid(), ManagementAutonomy.Supervised);
        var club = new ClubId(Guid.NewGuid());
        var responsibility = new ClubResponsibility("training", "first-team");

        assignment.Validate(club, responsibility, (owner, code, employee) =>
            owner == club && code == responsibility.Code && employee == assignment.EmployeeId);
        Assert.Equal(ResponsibilityAssignment.Personal,
            assignment.EffectiveFor(club, responsibility, (_, _, _) => false));
    }
}
