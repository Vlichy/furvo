#:project ../src/Furvo.Persistence/Furvo.Persistence.csproj
#:property PublishAot=false

using Furvo.Persistence;

// Utilidad de desarrollo .NET 10; no añade un proyecto ni una pantalla al juego.
// Esta utilidad acepta solo el destino: la generación y su validación pertenecen a Persistence.
if (args.Length > 1)
{
    Console.Error.WriteLine("Uso: dotnet run --file tools/BuildInitialWorld.cs -- [ruta-destino]");
    return 2;
}
try
{
    // Resolver desde la carpeta de trabajo permite usar el proyecto en otro equipo sin rutas incrustadas.
    var destination = Path.GetFullPath(args.Length == 1 ? args[0] : Path.Combine("data", "initial-world-2026-27.v2.db"));
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var world = InitialWorld2026.LoadOrCreate(destination);
    var data = world.Load();
    Console.WriteLine($"Mundo inicial creado/verificado: {destination}");
    Console.WriteLine($"{data.Clubs.Count} clubes, {data.Competitions.Count} competición, {data.Seasons.Count} temporada, {data.Players.Count} jugadores.");
    return 0;
}
// Los errores se comunican con código de salida no nulo; nunca se intenta forzar una sobrescritura.
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
