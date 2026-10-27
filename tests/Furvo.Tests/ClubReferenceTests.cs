using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

/// <summary>Comprueba la correspondencia editorial sin incorporar nombres reales a los datos comerciales.</summary>
public sealed class ClubReferenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoReferenceTests", Guid.NewGuid().ToString("N"));

    /// <summary>Aísla los archivos de esta prueba de cualquier publicación o partida del usuario.</summary>
    public ClubReferenceTests() => Directory.CreateDirectory(_directory);

    private string Database(string name) => Path.Combine(_directory, name + ".db");
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    // Lista aprobada independiente del manifiesto: detecta sustituciones, duplicados y ausencias en el mapa de desarrollo.
    private static readonly string[] ExpectedReferences =
    [
        "Athletic Club", "Atlético de Madrid", "CA Osasuna", "RC Celta de Vigo", "Deportivo Alavés",
        "Elche CF", "FC Barcelona", "Getafe CF", "Levante UD", "Málaga CF", "Racing de Santander",
        "Rayo Vallecano", "Deportivo de La Coruña", "RCD Espanyol", "Real Betis", "Real Madrid",
        "Real Sociedad", "Sevilla FC", "Valencia CF", "Villarreal CF"
    ];

    private static JsonDocument Mapping() => JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "club-reference-2026-27.json")));

    // El origen anterior se copia solo al directorio de Tests; el juego distribuye únicamente la publicación vigente.
    private static JsonDocument Resource(string file)
    {
        if (file == "initial-world-2026-27.json")
            return JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "initial-world-2026-27.v1.json")));
        using var stream = typeof(InitialWorld2026).Assembly.GetManifestResourceStream("Furvo.Persistence.Data." + file)!;
        return JsonDocument.Parse(stream);
    }

    /// <summary>Exige una biyección exacta con los veinte participantes aprobados y sus datos ficticios revisados.</summary>
    [Fact]
    public void EveryRequiredReferenceHasExactlyOneMatchingFictionalClub()
    {
        using var mapping = Mapping();
        var rows = mapping.RootElement.GetProperty("Clubs").EnumerateArray().ToArray();
        Assert.Equal(20, rows.Length);
        var names = rows.Select(x => x.GetProperty("ReferenceClub").GetString()!).ToArray();
        Assert.Equal(20, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ExpectedReferences.OrderBy(x => x), names.OrderBy(x => x));
        var ids = rows.Select(x => Guid.Parse(x.GetProperty("FurvoClubId").GetString()!)).ToArray();
        Assert.Equal(20, ids.Distinct().Count());

        var world = InitialWorld2026.LoadOrCreate(Database("corrected"));
        var data = world.Load();
        Assert.Equal(20, data.Clubs.Count);
        Assert.Equal(data.Clubs.Select(x => x.Id.Value).OrderBy(x => x), ids.OrderBy(x => x));
        foreach (var row in rows)
        {
            var club = Assert.Single(data.Clubs, x => x.Id.Value == Guid.Parse(row.GetProperty("FurvoClubId").GetString()!));
            Assert.Equal(row.GetProperty("FurvoName").GetString(), club.Name);
            Assert.Equal(row.GetProperty("City").GetString(), club.City);
            Assert.Equal(row.GetProperty("Reputation").GetInt32(), club.Reputation);
            Assert.Equal(row.GetProperty("Stadium").GetString(), club.Profile!.StadiumName);
            Assert.Equal(row.GetProperty("Capacity").GetInt32(), club.Profile.StadiumCapacity);
            Assert.InRange(club.Reputation!.Value, 1, 5);
        }
        Assert.Equal("Liga Cumbre de España", Assert.Single(data.Competitions).Name);
        Assert.Equal("2026/27", Assert.Single(data.Seasons).Name);
        Assert.Equal(20, data.Registrations.Count);
        Assert.Empty(data.Players);
        Assert.Empty(data.Contracts);
    }

    /// <summary>La condición de ascendido es descriptiva y corresponde solo a las tres referencias verificadas.</summary>
    [Fact]
    public void PromotedClubsMatchTheOfficialReferencesWithoutAddingPromotionMechanics()
    {
        using var mapping = Mapping();
        var rows = mapping.RootElement.GetProperty("Clubs").EnumerateArray().ToArray();
        var promoted = rows.Where(x => x.GetProperty("NewlyPromoted").GetBoolean()).ToArray();
        Assert.Equal(new[] { "Deportivo de La Coruña", "Málaga CF", "Racing de Santander" },
            promoted.Select(x => x.GetProperty("ReferenceClub").GetString()!).OrderBy(x => x).ToArray());
        var clubs = InitialWorld2026.LoadOrCreate(Database("promoted")).Load().Clubs;
        foreach (var row in rows)
        {
            var club = clubs.Single(x => x.Id.Value == Guid.Parse(row.GetProperty("FurvoClubId").GetString()!));
            Assert.Equal(row.GetProperty("NewlyPromoted").GetBoolean(),
                club.Profile!.SportingIdentity.Contains("Recién ascendido", StringComparison.Ordinal));
        }
    }

    /// <summary>Conserva ocho identidades y evita reasignar los IDs retirados a clubes de otras ciudades.</summary>
    [Fact]
    public void RetainedClubIdsRemainStableAndReplacementsHaveNewIdentities()
    {
        using var previous = Resource("initial-world-2026-27.json");
        using var current = Resource("initial-world-2026-27.v2.json");
        var oldClubs = previous.RootElement.GetProperty("Clubs").EnumerateArray().ToDictionary(
            x => x.GetProperty("Id").GetString()!, x => x.GetProperty("Name").GetString()!);
        var newClubs = current.RootElement.GetProperty("Clubs").EnumerateArray().ToArray();
        var retained = newClubs.Where(x => oldClubs.ContainsKey(x.GetProperty("Id").GetString()!)).ToArray();
        Assert.Equal(8, retained.Length);
        foreach (var club in retained)
            Assert.Equal(oldClubs[club.GetProperty("Id").GetString()!], club.GetProperty("Name").GetString());
        Assert.Equal(12, newClubs.Count(x => !oldClubs.ContainsKey(x.GetProperty("Id").GetString()!)));
        Assert.NotEqual(previous.RootElement.GetProperty("WorldId").GetString(),
            current.RootElement.GetProperty("WorldId").GetString());
        Assert.Equal(previous.RootElement.GetProperty("Competition").GetProperty("Id").GetString(),
            current.RootElement.GetProperty("Competition").GetProperty("Id").GetString());
        Assert.Equal(previous.RootElement.GetProperty("Season").GetProperty("Id").GetString(),
            current.RootElement.GetProperty("Season").GetProperty("Id").GetString());
    }

    /// <summary>El mapa real se copia solo a Tests; el recurso de Persistence contiene exclusivamente las identidades ficticias.</summary>
    [Fact]
    public void RealReferencesAreAbsentFromCommercialDataAndDevelopmentFilesAreNotEmbedded()
    {
        using var current = Resource("initial-world-2026-27.v2.json");
        var text = current.RootElement.GetRawText();
        Assert.DoesNotContain("ReferenceClub", text);
        foreach (var name in ExpectedReferences) Assert.DoesNotContain(name, text);
        Assert.DoesNotContain(typeof(InitialWorld2026).Assembly.GetManifestResourceNames(),
            x => x.Contains("club-reference", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Furvo.Persistence.Data.initial-world-2026-27.json",
            typeof(InitialWorld2026).Assembly.GetManifestResourceNames());
        Assert.Equal(61, PlayerAttributeCatalog.All.Count);
        var attributes = new PlayerAttributes(PlayerAttributeCatalog.All.Select(x => new KeyValuePair<string, int>(x.Code, 20)));
        Assert.Equal(51, attributes.VisibleValues.Count);
    }

    /// <summary>La corrección rechaza una publicación anterior y no toca tampoco las partidas creadas con ella.</summary>
    [Fact]
    public void CorrectionDoesNotOverwriteAnOlderWorldOrItsIndependentSaves()
    {
        var oldPath = Database("older-publication");
        var club = new Club(ClubId.New(), "Publicación de prueba", "Ciudad", "España", 3);
        var older = SqliteWorldFiles.CreateInitialWorld(oldPath, WorldId.New(), new WorldData(clubs: [club]));
        var first = SqliteWorldFiles.CreateSave(oldPath, Database("first"), SaveId.New());
        var second = SqliteWorldFiles.CreateSave(oldPath, Database("second"), SaveId.New());
        var oldHash = Hash(oldPath);
        var firstHash = Hash(Database("first"));
        var secondHash = Hash(Database("second"));
        Assert.Throws<InvalidOperationException>(() => InitialWorld2026.LoadOrCreate(oldPath));
        var corrected = InitialWorld2026.LoadOrCreate(Database("corrected"));
        Assert.Equal(20, corrected.Load().Clubs.Count);
        Assert.Equal(oldHash, Hash(oldPath));
        Assert.Equal(firstHash, Hash(Database("first")));
        Assert.Equal(secondHash, Hash(Database("second")));
        Assert.Equal(club, Assert.Single(older.Load().Clubs));
        Assert.Equal(club, Assert.Single(first.Load().Clubs));
        Assert.Equal(club, Assert.Single(second.Load().Clubs));
    }

    /// <summary>Elimina exclusivamente los archivos temporales creados por esta instancia de prueba.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
