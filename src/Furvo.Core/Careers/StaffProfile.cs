using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Core.Careers;

public enum StaffContractStatus { Unknown, Free, Contracted }

/// <summary>Proyección pública. Los datos profesionales ausentes conservan su estado desconocido.</summary>
public sealed record StaffProfile(Guid Id, string Name, ClubId? Club, string Role, string? Team,
    DateOnly? BirthDate = null, string? Nationality = null, int? Reputation = null,
    ManagerEmploymentTerms? Contract = null, StaffProfession? Profession = null,
    StaffEmployment? Employment = null, bool ConfirmedFree = false)
{
    public StaffContractStatus ContractStatus(DateOnly date) =>
        Contract is { StartDate: { } start, EndDate: { } end }
            ? date >= start && date <= end ? StaffContractStatus.Contracted
                : date > end ? StaffContractStatus.Free : StaffContractStatus.Unknown
            : ConfirmedFree ? StaffContractStatus.Free : StaffContractStatus.Unknown;
}

public static class StaffSearch
{
    public static IReadOnlyList<StaffProfile> Find(IEnumerable<StaffProfile> staff, DateOnly date, string name,
        StaffContractStatus? status = null, int? expiryMonths = null, string? role = null, int? reputation = null)
    {
        if (expiryMonths is not (null or 3 or 6 or 12)) throw new ArgumentOutOfRangeException(nameof(expiryMonths));
        if (reputation is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(reputation));
        var query = MarketQueries.NormalizeName(name);
        var cutoff = expiryMonths is { } months ? date.AddMonths(months) : (DateOnly?)null;
        return staff.Where(p => MarketQueries.NormalizeName(p.Name).Contains(query, StringComparison.Ordinal)
                && (role is null || p.Role == role)
                && (reputation is null || p.Reputation == reputation)
                && (status is null || p.ContractStatus(date) == status)
                && (cutoff is null || p.ContractStatus(date) == StaffContractStatus.Contracted && p.Contract!.EndDate <= cutoff))
            .OrderBy(p => p.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("es-ES"), true))
            .ThenBy(p => p.Id).ToArray();
    }
}
