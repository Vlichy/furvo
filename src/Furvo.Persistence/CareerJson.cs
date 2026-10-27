using System.Text.Json;
using System.Text.Json.Serialization;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Serialización versionada de instantáneas; los GUID tipados se conservan sin recurrir a constructores vacíos.</summary>
internal static class CareerJson
{
    private sealed class IdConverter<T>(Func<Guid,T> create,Func<T,Guid> value):JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>create(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer,T item,JsonSerializerOptions options)=>writer.WriteStringValue(value(item));
    }
    private static JsonSerializerOptions Options {get;}=Build();
    private static JsonSerializerOptions Build()
    {
        var o=new JsonSerializerOptions();
        o.Converters.Add(new IdConverter<CoachProfileId>(g=>new(g),x=>x.Value));
        o.Converters.Add(new IdConverter<CoachId>(g=>new(g),x=>x.Value));
        o.Converters.Add(new IdConverter<SaveId>(g=>new(g),x=>x.Value));
        o.Converters.Add(new IdConverter<ClubId>(g=>new(g),x=>x.Value));
        o.Converters.Add(new IdConverter<SeasonId>(g=>new(g),x=>x.Value));
        o.Converters.Add(new IdConverter<ManagerContractId>(g=>new(g),x=>x.Value)); return o;
    }
    internal static string Write<T>(T value)=>JsonSerializer.Serialize(value,Options);
    internal static T Read<T>(string json)=>JsonSerializer.Deserialize<T>(json,Options)??throw new InvalidDataException("Instantánea vacía.");
    internal static void Migrate(SqliteConnection c,SqliteTransaction t,string resource)
    {
        using var source=typeof(CareerJson).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations."+resource)??throw new InvalidOperationException("Falta migración.");
        using var reader=new StreamReader(source); using var command=WorldDatabase.Command(c,t,reader.ReadToEnd()); command.ExecuteNonQuery();
    }
}
