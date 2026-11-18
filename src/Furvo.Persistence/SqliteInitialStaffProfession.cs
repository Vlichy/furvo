using Furvo.Core.Careers;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

internal static class SqliteInitialStaffProfession
{
    internal static void Create(SqliteConnection connection, SqliteTransaction transaction, WorldData world, DateOnly date)
    {
        CareerJson.Migrate(connection, transaction, "StaffProfession001_Initial.sql");
        var teams = AcademyCatalog.Teams;
        var counts = teams.GroupBy(team => team.ClubId).ToDictionary(group => group.Key, group => group.Count());
        var economy = SqliteEconomyStore.ReadResources(connection, transaction)
            .Where(row => world.Seasons.Any(season => season.Id == row.SeasonId && season.StartDate <= date && date <= season.EndDate))
            .ToDictionary(row => row.ClubId, row => row.EconomicPlan);
        var end = world.Seasons.Single(season => season.StartDate <= date && date <= season.EndDate).EndDate;
        var cultures = InitialSquads2026.Settings().Cultures;
        foreach (var team in teams)
        {
            var coach = Furvo.Core.Academy.AcademyGenerator.Coach(team, cultures);
            var profile = InitialStaffProfession.Generate(coach, team, date);
            using var employee = WorldDatabase.Command(connection, transaction,
                "INSERT INTO staff_professions VALUES($id,1,$profile)",
                ("$id", coach.Id.ToString("D")), ("$profile", CareerJson.Write(profile)));
            employee.ExecuteNonQuery();
            var contract = InitialStaffProfession.Employment(coach.Id, team, profile, date, end,
                economy.GetValueOrDefault(team.ClubId), counts[team.ClubId]);
            if (contract is null) continue;
            using var employment = WorldDatabase.Command(connection, transaction,
                "INSERT INTO staff_employments VALUES($id,$employee,$club,$contract)",
                ("$id", contract.Id.ToString("D")), ("$employee", coach.Id.ToString("D")),
                ("$club", team.ClubId.ToString()), ("$contract", CareerJson.Write(contract)));
            employment.ExecuteNonQuery();
        }
    }
}
