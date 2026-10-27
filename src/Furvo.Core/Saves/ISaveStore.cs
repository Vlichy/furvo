using Furvo.Core.Domain;

namespace Furvo.Core.Saves;

/// <summary>Identidad de partida y versión de formato; se pueden consultar sin cargar todo el mundo.</summary>
public sealed record SaveMetadata(SaveId Id, int SchemaVersion);

// Cada instancia se vincula a una única partida/mundo persistente.
/// <summary>Puerto mínimo de metadatos, independiente de SQLite y de las carpetas de Godot.</summary>
public interface ISaveStore
{
    /// <summary>Crea una partida vacía o verifica la identidad existente; no debe convertir silenciosamente partidas anteriores.</summary>
    SaveMetadata Initialize(SaveId id);
    /// <summary>Consulta la cabecera de una partida; una ruta ausente o sin inicializar no representa una partida.</summary>
    SaveMetadata? ReadMetadata();
}
