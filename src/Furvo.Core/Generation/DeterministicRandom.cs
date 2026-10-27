using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Furvo.Core.Generation;

/// <summary>Flujo SplitMix64 explícito: no depende de System.Random, del reloj ni de su implementación futura.</summary>
internal sealed class DeterministicRandom
{
    private ulong _state;

    internal DeterministicRandom(string context)
    {
        _state = BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(context)));
    }

    /// <summary>Genera un entero acotado. Cada jugador recibe un flujo separado, estable por identidad y semilla.</summary>
    internal int Next(int minimum, int maximumExclusive)
    {
        if (minimum >= maximumExclusive) throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
        unchecked
        {
            var z = (_state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return minimum + (int)(z % (uint)(maximumExclusive - minimum));
        }
    }

    /// <summary>GUID de versión 8 derivado de SHA-256; nunca usa Guid.NewGuid ni depende del nombre generado.</summary>
    internal static Guid Identity(string context)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(context));
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return Guid.ParseExact(Convert.ToHexString(bytes.AsSpan(0, 16)), "N");
    }

    internal static string Context(SquadGenerationSettings settings, string suffix) =>
        settings.Version + "|" + settings.Seed.ToString(CultureInfo.InvariantCulture) + "|" + suffix;
}
