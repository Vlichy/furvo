using System.Security.Cryptography;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;

namespace Furvo.Persistence;

/// <summary>Abre publicaciones para consulta o materializa una copia de distribución; nunca crea partidas ni migra bases.</summary>
public static class PublishedWorldQueries
{
    /// <summary>Carga SQLite en modo de solo lectura y proyecta al comienzo de la temporada disponible.</summary>
    public static IWorldQueries Open(string databasePath)
    {
        var store = new SqliteWorldStore(databasePath);
        if (store.ReadInfo().Kind != WorldFileKind.InitialWorld)
            throw new InvalidDataException("Se requiere una publicación inicial, no una partida.");
        var data = store.Load();
        if (data.Seasons.Count != 1) throw new InvalidDataException("La publicación debe identificar una temporada inicial.");
        var economic=SqliteEconomyStore.ReadResources(databasePath);
        if(economic.Count!=0 && (economic.Count!=data.Clubs.Count || economic.Any(e=>e.SeasonId!=data.Seasons[0].Id)))throw new InvalidDataException("Publicación económica incompleta.");
        return new WorldQueries(data, data.Seasons[0].StartDate,economic);
    }

    /// <summary>Materializa bytes de la publicación incluida en el ensamblado en una caché identificada por SHA-256.</summary>
    public static string Materialize(Stream publication, string cacheDirectory)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (!Path.IsPathFullyQualified(cacheDirectory)) throw new ArgumentException("Se requiere una carpeta absoluta.", nameof(cacheDirectory));
        using var bytes = new MemoryStream();
        publication.CopyTo(bytes);
        var content = bytes.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        Directory.CreateDirectory(cacheDirectory);
        var destination = Path.Combine(cacheDirectory, "initial-world-" + hash + ".db");
        // El nombre temporal no determina ninguna identidad del mundo; solo aísla la operación de publicación local.
        var temporary = Path.Combine(cacheDirectory, ".catalog-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            if (!File.Exists(destination))
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    output.Write(content);
                try { File.Move(temporary, destination, overwrite: false); }
                catch (IOException) when (File.Exists(destination)) { /* Otra instancia terminó antes: verificar sus bytes. */ }
            }
            using var existing = File.OpenRead(destination);
            if (!SHA256.HashData(existing).SequenceEqual(SHA256.HashData(content)))
                throw new InvalidDataException("La copia local del catálogo no coincide con la publicación; no se sobrescribe.");
            return destination;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
