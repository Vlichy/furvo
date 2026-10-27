#:project ../src/Furvo.Persistence/Furvo.Persistence.csproj
#:property PublishAot=false

using Furvo.Persistence;

// Materializa otra publicación sin modificar las anteriores ni regenerar jugadores.
if (args.Length > 2)
{
    Console.Error.WriteLine("Uso: dotnet run --file tools/BuildAdjustedWorld.cs -- [destino-v4] [origen-v3]");
    return 2;
}
try
{
    var destination = Path.GetFullPath(args.Length > 0 ? args[0] : "data/initial-world-2026-27.v4.db");
    var source = Path.GetFullPath(args.Length > 1 ? args[1] : "data/initial-world-2026-27.v3.db");
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var world = AdjustedInitialWorld2026.LoadOrCreate(source, destination);
    var data = world.Load();
    Console.WriteLine($"Publicación v4 creada/verificada: {destination}");
    Console.WriteLine($"WorldId {world.ReadInfo().WorldId}; {data.Clubs.Count} clubes; {data.Players.Count} jugadores; {data.Contracts.Count} contratos.");
    Console.WriteLine($"Huella {InitialSquads2026.Fingerprint(data)}");
    var fragua = AdjustedInitialWorld2026.ReadReputations().Single(x => x.Name == "Fragua del Sur");
    Console.WriteLine($"Fragua del Sur: interna {fragua.Internal}/100; visible {fragua.Visible}/5.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
