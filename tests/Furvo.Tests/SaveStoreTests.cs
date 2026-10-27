using Furvo.Core.Domain;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Pruebas con SQLite real y directorio temporal exclusivo; no abren ni modifican partidas del usuario.</summary>
public sealed class SaveStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoTests", Guid.NewGuid().ToString("N"));

    /// <summary>Prepara un directorio propio para aislar esta prueba de ejecuciones paralelas.</summary>
    public SaveStoreTests() => Directory.CreateDirectory(_directory);
    private string Database(string name) => Path.Combine(_directory, name + ".db");

    /// <summary>Preserva la identidad al reabrir y rechaza reutilizar el archivo para otro SaveId.</summary>
    [Fact]
    public void IdentitySurvivesReopeningAndInitializationIsIdempotent()
    {
        var path = Database("save");
        var id = SaveId.New();
        var store = new SqliteSaveStore(path);
        Assert.Null(store.ReadMetadata());
        var expected = store.Initialize(id);
        Assert.Equal(expected, store.Initialize(id));
        Assert.Equal(expected, new SqliteSaveStore(path).ReadMetadata());
        Assert.Throws<InvalidOperationException>(() => store.Initialize(SaveId.New()));
        Assert.Equal(expected, store.ReadMetadata());
    }

    /// <summary>Comprueba que archivos distintos mantienen sus propias identidades de partida.</summary>
    [Fact]
    public void IndependentFilesKeepIndependentWorldIdentities()
    {
        var first = new SqliteSaveStore(Database("first"));
        var second = new SqliteSaveStore(Database("second"));
        var firstId = SaveId.New();
        var secondId = SaveId.New();
        first.Initialize(firstId);
        second.Initialize(secondId);
        Assert.Equal(firstId, first.ReadMetadata()!.Id);
        Assert.Equal(secondId, second.ReadMetadata()!.Id);
    }

    /// <summary>Evita que una versión desconocida se reinterprete o se degrade para poder abrirla.</summary>
    [Fact]
    public void FutureSchemaIsRejectedWithoutDowngrading()
    {
        var path = Database("future");
        var store = new SqliteSaveStore(path);
        var id = SaveId.New();
        store.Initialize(id);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version = 99;";
        command.ExecuteNonQuery();
        Assert.Throws<NotSupportedException>(() => store.Initialize(id));
        Assert.Throws<NotSupportedException>(() => store.ReadMetadata());
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(99L, (long)command.ExecuteScalar()!);
    }

    /// <summary>Comprueba formato estable e identidades vacías, incluido el default de structs.</summary>
    [Fact]
    public void IdentifiersRoundTripAndRejectEmptyValues()
    {
        var id = ClubId.New();
        Assert.Equal(id, new ClubId(Guid.Parse(id.ToString())));
        Assert.Throws<ArgumentException>(() => new ClubId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new PlayerId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new ContractId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new SaveId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new SqliteSaveStore(Database("empty")).Initialize(default));
    }

    /// <summary>Elimina únicamente el directorio temporal creado por esta instancia de prueba.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
