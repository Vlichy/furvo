using Furvo.Core.Careers;
using Furvo.Core.Domain;

namespace Furvo.Persistence;

/// <summary>Directorio de lectura de las identidades ya persistidas en la carrera.</summary>
public static class SqliteStaffDirectory
{
    internal static IReadOnlyDictionary<Guid, StaffProfession> TrainingProfiles(string path, Guid team, DateOnly date)
    {
        using var connection = WorldDatabase.Open(path);
        using var transaction = connection.BeginTransaction(deferred: true);
        using var schema = WorldDatabase.Command(connection, transaction,
            "SELECT count(*) FROM sqlite_master WHERE name='staff_professions'");
        var profiles = new Dictionary<Guid, StaffProfession>();
        if (Convert.ToInt32(schema.ExecuteScalar()) == 0) return profiles;
        using var query = WorldDatabase.Command(connection, transaction,
            "SELECT s.id,p.profile_json,e.contract_json FROM academy_staff s JOIN staff_professions p ON p.employee_id=s.id LEFT JOIN staff_employments e ON e.employee_id=s.id WHERE s.team_id=$team AND s.link_state='Institutional' AND p.version=1",
            ("$team", team.ToString("D")));
        using var reader = query.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(2))
            {
                var employment = CareerJson.Read<StaffEmployment>(reader.GetString(2));
                if (date < employment.Start || date > employment.End) continue;
            }
            var profile = CareerJson.Read<StaffProfession>(reader.GetString(1));
            profile.Validate(date);
            profiles.Add(Guid.Parse(reader.GetString(0)), profile);
        }
        return profiles;
    }

    public static IReadOnlyList<StaffProfile> Read(string path, CareerSummary career)
    {
        using var connection = WorldDatabase.Open(path);
        using var transaction = connection.BeginTransaction(deferred: true);
        var owner = SqliteLineupStore.Owner(connection, transaction);
        if (owner.Save != career.Id || owner.Club != career.ClubId)
            throw new InvalidOperationException("El directorio pertenece a otra carrera.");
        var profile = career.Coach.ProfileSnapshot;
        var rows = new List<StaffProfile>
        {
            new(career.Coach.Id.Value, profile.FullName, owner.Club, "Manager", null,
                profile.BirthDate, profile.NationalityCode, Contract: career.Contract?.AcceptedOffer.Terms,
                ConfirmedFree: career.Employment == CareerEmploymentState.Unemployed)
        };
        using var schema = WorldDatabase.Command(connection, transaction,
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='academy_staff'");
        if (Convert.ToInt32(schema.ExecuteScalar()) == 0) return rows;
        using var extension = WorldDatabase.Command(connection, transaction,
            "SELECT count(*) FROM sqlite_master WHERE name IN ('staff_professions','staff_employments')");
        var tables = Convert.ToInt32(extension.ExecuteScalar());
        if (tables == 1) throw new InvalidDataException("Extensión de Staff incompleta.");
        var projection = tables == 2 ? ",p.version,p.profile_json,e.id,e.club_id,e.contract_json" : "";
        var join = tables == 2 ? " LEFT JOIN staff_professions p ON p.employee_id=s.id LEFT JOIN staff_employments e ON e.employee_id=s.id" : "";
        using var query = WorldDatabase.Command(connection, transaction,
            "SELECT s.id,s.name,t.club_id,t.name" + projection +
            " FROM academy_staff s JOIN academy_teams t ON t.id=s.team_id" + join +
            " WHERE s.link_state='Institutional' AND s.role='HeadCoach'");
        using var reader = query.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var club = new ClubId(Guid.Parse(reader.GetString(2)));
            StaffProfession? profession = null;
            StaffEmployment? contract = null;
            if (tables == 2 && !reader.IsDBNull(4))
            {
                if (reader.GetInt32(4) != 1) throw new NotSupportedException("Versión de Staff incompatible.");
                profession = CareerJson.Read<StaffProfession>(reader.GetString(5));
                profession.Validate(career.CurrentDate);
                if (!reader.IsDBNull(8))
                {
                    contract = CareerJson.Read<StaffEmployment>(reader.GetString(8));
                    contract.Validate(id, club);
                    if (contract.Id != Guid.Parse(reader.GetString(6)) || club.Value != Guid.Parse(reader.GetString(7)))
                        throw new InvalidDataException("Identidad contractual incoherente.");
                }
            }
            rows.Add(new(id, reader.GetString(1), club, "AcademyCoach", reader.GetString(3),
                profession?.BirthDate, profession?.Nationality, profession?.Reputation, contract?.DisplayTerms(), profession, contract));
        }
        return rows;
    }
}
