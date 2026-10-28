#:project ../src/Furvo.Persistence/Furvo.Persistence.csproj
#:property PublishAot=false
using Furvo.Persistence;
if(args.Length>2)return 2;
try
{
    var destination=Path.GetFullPath(args.Length>0?args[0]:"data/initial-world-2026-27.v5.db");
    var source=Path.GetFullPath(args.Length>1?args[1]:"data/initial-world-2026-27.v4.db");
    var world=EconomicInitialWorld2026.LoadOrCreate(source,destination);
    Console.WriteLine($"Publicación v5 creada/verificada: {world.ReadInfo().WorldId}; {world.Load().Players.Count} jugadores; {SqliteEconomyStore.ReadResources(destination).Count} economías.");return 0;
}
catch(Exception e){Console.Error.WriteLine(e);return 1;}
