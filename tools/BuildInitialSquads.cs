#:project ../src/Furvo.Persistence/Furvo.Persistence.csproj
#:property PublishAot=false

using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Persistence;

// Utilidad de publicación, sin crear otro proyecto ni modificar el arranque del juego.
if (args.Length > 1)
{
    Console.Error.WriteLine("Uso: dotnet run --file tools/BuildInitialSquads.cs -- [ruta-destino]");
    return 2;
}
try
{
    var destination = Path.GetFullPath(args.Length == 1 ? args[0] : Path.Combine("data", "initial-world-2026-27.v3.db"));
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var store = InitialSquads2026.LoadOrCreate(destination);
    var data = store.Load();
    var season = data.Seasons.Single();
    var settings = InitialSquads2026.Settings();
    Console.WriteLine($"Publicación v3 creada/verificada: {destination}");
    Console.WriteLine($"{data.Clubs.Count} clubes, {data.Players.Count} jugadores, {data.Contracts.Count} contratos.");
    Console.WriteLine($"Generador {settings.Version}; semilla {settings.Seed}; huella {InitialSquads2026.Fingerprint(data)}");
    // Informe de desarrollo: ejemplos solo con atributos visibles, sin revelar los diez ocultos.
    var report = new
    {
        GeneratorVersion = settings.Version, settings.Seed, WorldId = store.ReadInfo().WorldId.ToString(),
        Fingerprint = InitialSquads2026.Fingerprint(data),
        Players = data.Players.Count, Contracts = data.Contracts.Count,
        AttributeValues = data.Players.Sum(x => x.Attributes.AllValues.Count),
        Clubs = data.Clubs.OrderBy(x => x.Name, StringComparer.Ordinal).Select(club =>
        {
            var ids = data.Contracts.Where(x => x.ClubId == club.Id).Select(x => x.PlayerId).ToHashSet();
            var squad = data.Players.Where(x => ids.Contains(x.Id)).ToArray();
            return new
            {
                club.Name, club.Reputation, ClubId = club.Id.ToString(), Players = squad.Length,
                Goalkeepers = squad.Count(x => x.MainPosition == PlayerPosition.Goalkeeper),
                Defenders = squad.Count(x => x.MainPosition == PlayerPosition.Defender),
                Midfielders = squad.Count(x => x.MainPosition == PlayerPosition.Midfielder),
                Forwards = squad.Count(x => x.MainPosition == PlayerPosition.Forward)
            };
        }),
        Ages = data.Players.GroupBy(x => x.AgeOn(season.StartDate)).OrderBy(x => x.Key).Select(x => new { Age = x.Key, Count = x.Count() }),
        Nationalities = data.Players.GroupBy(x => x.Nationality).OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new { Country = x.Key, Count = x.Count() }),
        Examples = data.Players.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).GroupBy(x => x.Positions!.Primary)
            .Select(x => x.First()).Select(x => new
            {
                PlayerId = x.Id.ToString(), x.Name, x.Nationality, x.BirthDate, Age = x.AgeOn(season.StartDate),
                Position = x.Positions!.Primary.ToString(), Secondary = x.Positions.Secondary.Select(v => v.ToString()),
                Club = data.Clubs.Single(c => c.Id == data.Contracts.Single(c => c.PlayerId == x.Id).ClubId).Name,
                VisibleAttributes = x.Attributes.VisibleValues
            })
    };
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error)
{
    // La publicación previa permanece intacta; el llamador decide otra ruta o revisa el conflicto.
    Console.Error.WriteLine(error.Message);
    return 1;
}
