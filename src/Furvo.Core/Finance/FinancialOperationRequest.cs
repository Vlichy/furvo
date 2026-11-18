using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Furvo.Core.Domain;

namespace Furvo.Core.Finance;

/// <summary>Solicitud técnica. Terms debe conservar la representación canónica versionada de todos los términos.</summary>
public sealed record FinancialOperationRequest(Guid Id, SaveId Career, string Kind, string Origin, string Terms)
{
    public string Fingerprint()
    {
        if (Id == Guid.Empty || Career.Value == Guid.Empty || string.IsNullOrWhiteSpace(Kind) ||
            string.IsNullOrWhiteSpace(Origin) || string.IsNullOrWhiteSpace(Terms))
            throw new ArgumentException("La operación requiere identidad, carrera, tipo, origen y términos.");
        // El array delimita los campos sin ambigüedad y sin depender de la cultura del ordenador.
        var payload = JsonSerializer.Serialize(new[] { Career.ToString(), Kind, Origin, Terms });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}

/// <summary>El resultado solo es durable cuando el llamador confirma su transacción.</summary>
public sealed record FinancialOperationResult(string ResultJson, bool Replayed);
