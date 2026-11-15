using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;

/// <summary>Preferencias visuales del usuario, ajenas a las carreras. Solo acepta códigos públicos, nunca entidades.</summary>
public sealed class MarketColumnPreferences
{
    private readonly Dictionary<string,string[]> _positions=new(StringComparer.Ordinal);
    public const int FormatVersion=1;
    public static string Key(PlayerRole? position)
    {
        if(position is {} role&&!Enum.IsDefined(role))throw new ArgumentOutOfRangeException(nameof(position));
        return position?.ToString()??"All";
    }
    public static IReadOnlyList<string> Defaults(PlayerRole? position)
    {
        _=Key(position);
        return position is {} role?PositionInsightCatalog.RelevantFor(role).ToArray():new[]{"ball_control","short_passing","decisions","pace","stamina"};
    }
    public IReadOnlyList<string> Columns(PlayerRole? position)=>_positions.TryGetValue(Key(position),out var columns)?Array.AsReadOnly(columns):Defaults(position);
    public void Set(PlayerRole? position,IEnumerable<string> codes)
    {
        var values=codes.ToArray();
        foreach(var code in values)MarketQueries.RequirePublicAttribute(code);
        if(values.Distinct(StringComparer.Ordinal).Count()!=values.Length)throw new InvalidDataException("Columnas repetidas.");
        _positions[Key(position)]=values;
    }
    public void Reset(PlayerRole? position)=>_positions.Remove(Key(position));
    public static IReadOnlyList<string> Move(IReadOnlyList<string> columns,int index,int direction)
    {
        if(index<0||index>=columns.Count||direction is not(-1 or 1)||index+direction<0||index+direction>=columns.Count)throw new ArgumentOutOfRangeException(nameof(index));
        var result=columns.ToArray();(result[index],result[index+direction])=(result[index+direction],result[index]);return result;
    }
    public string Serialize()=>JsonSerializer.Serialize(new Document(FormatVersion,_positions));
    public static MarketColumnPreferences Deserialize(string json)
    {
        var document=JsonSerializer.Deserialize<Document>(json)??throw new InvalidDataException("Preferencias vacías.");
        if(document.Version!=FormatVersion||document.Positions is null)throw new InvalidDataException("Versión de preferencias no compatible.");
        var result=new MarketColumnPreferences();
        foreach(var (key,columns) in document.Positions){
            PlayerRole? role=null;
            if(key!="All"){if(!Enum.TryParse<PlayerRole>(key,out var parsed)||!Enum.IsDefined(parsed)||parsed.ToString()!=key)throw new InvalidDataException("Posición desconocida en preferencias.");role=parsed;}
            if(columns is null)throw new InvalidDataException("Lista de columnas desconocida.");
            result.Set(role,columns);
        }
        return result;
    }
    public static MarketColumnPreferences Load(string path)=>File.Exists(path)?Deserialize(File.ReadAllText(path)):new();
    // Sustitución atómica en el mismo directorio: no toca SQLite ni deja un archivo truncado al guardar.
    public void Save(string path)
    {
        var directory=Path.GetDirectoryName(Path.GetFullPath(path))!;Directory.CreateDirectory(directory);
        var temporary=Path.Combine(directory,Path.GetFileName(path)+"."+Guid.NewGuid().ToString("N")+".tmp");
        try{File.WriteAllText(temporary,Serialize());File.Move(temporary,path,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    public sealed record Document(int Version,Dictionary<string,string[]> Positions);
}
