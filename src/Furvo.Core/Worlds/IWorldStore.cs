using Furvo.Core.Domain;

namespace Furvo.Core.Worlds;

// Un lote de altas/actualizaciones, o una instantánea recuperada. No contiene borrados.
/// <summary>Instantánea o lote de altas y actualizaciones; mantiene colecciones propias y no expresa borrados.</summary>
public sealed class WorldData
{
    public IReadOnlyList<Club> Clubs { get; }
    public IReadOnlyList<Player> Players { get; }
    public IReadOnlyList<Contract> Contracts { get; }
    public IReadOnlyList<Coach> Coaches { get; }
    public IReadOnlyList<Competition> Competitions { get; }
    public IReadOnlyList<Season> Seasons { get; }
    public IReadOnlyList<Registration> Registrations { get; }

    /// <summary>Copia defensivamente las secuencias para que el llamador no cambie un lote mientras se guarda.</summary>
    public WorldData(IEnumerable<Club>? clubs = null, IEnumerable<Player>? players = null,
        IEnumerable<Contract>? contracts = null, IEnumerable<Coach>? coaches = null,
        IEnumerable<Competition>? competitions = null, IEnumerable<Season>? seasons = null,
        IEnumerable<Registration>? registrations = null)
    {
        Clubs = Array.AsReadOnly((clubs ?? []).ToArray());
        Players = Array.AsReadOnly((players ?? []).ToArray());
        Contracts = Array.AsReadOnly((contracts ?? []).ToArray());
        Coaches = Array.AsReadOnly((coaches ?? []).ToArray());
        Competitions = Array.AsReadOnly((competitions ?? []).ToArray());
        Seasons = Array.AsReadOnly((seasons ?? []).ToArray());
        Registrations = Array.AsReadOnly((registrations ?? []).ToArray());
    }
}

/// <summary>Distingue una plantilla inicial de una partida editable con estado independiente.</summary>
public enum WorldFileKind { InitialWorld, Save }
/// <summary>WorldId conserva el origen común; SaveId diferencia partidas que proceden de la misma plantilla.</summary>
public sealed record WorldFileInfo(WorldFileKind Kind, WorldId WorldId, SaveId? SaveId);

/// <summary>Puerto de persistencia del mundo; la presentación no necesita conocer SQL ni conexiones.</summary>
public interface IWorldStore
{
    /// <summary>Identifica el archivo antes de decidir si puede tratarse como plantilla o partida.</summary>
    WorldFileInfo ReadInfo();
    /// <summary>Recupera una instantánea coherente de entidades y atributos individuales.</summary>
    WorldData Load();
    /// <summary>Aplica un lote atómico; omitir entidades o atributos no solicita su eliminación.</summary>
    void Apply(WorldData changes);
}
