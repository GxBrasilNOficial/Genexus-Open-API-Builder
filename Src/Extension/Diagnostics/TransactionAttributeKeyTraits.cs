using System;
using Artech.Genexus.Common.Parts;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Critério compartilhado de autonumeração de parte de chave (Wizard flat e leitor hierárquico B095).
/// </summary>
internal static class TransactionAttributeKeyTraits
{
    /// <summary>
    /// Overload SDK: PK composta decide antes de consultar a propriedade (e antes do fail-open por Attribute null).
    /// </summary>
    public static bool IsAutonumber(TransactionAttribute item, int primaryKeyPartCount)
    {
        // GeneXus: autonumeração só existe em PK de um único campo. Em chave composta,
        // a contagem decide sem consultar a propriedade — inclusive se a leitura lançar.
        if (primaryKeyPartCount > 1)
        {
            return false;
        }

        try
        {
            if (item?.Attribute == null)
            {
                return true;
            }

            var value = item.Attribute.GetPropertyValueString("Autonumber")
                ?? item.Attribute.GetPropertyValueString("idAUTONUMBER");
            return IsAutonumberCore(primaryKeyPartCount, hasAttributeMetadata: true, value);
        }
        catch
        {
            // Em caso de dúvida ou exceção na leitura da propriedade, adota fallback conservador
            // (bloqueia o campo no CreateRequest / marca como autonumerada).
            return true;
        }
    }

    /// <summary>
    /// Núcleo puro para fixtures offline e para o adaptador SDK.
    /// GeneXus: autonumeração só existe em PK de um único campo. Em chave composta,
    /// nenhuma parte pode ser autonumerada — a contagem decide antes do fail-open por metadata ausente.
    /// Evidência 2026-08-06: Teste (PK=3, Autonumber='False') e NotaFiscal (PK=1, Autonumber='True').
    /// </summary>
    public static bool IsAutonumberCore(int primaryKeyPartCount, bool hasAttributeMetadata, string? autonumberPropertyValue)
    {
        // Contagem decide antes do fail-open: PK composta nunca é autonumerada, mesmo sem metadata.
        if (primaryKeyPartCount > 1)
        {
            return false;
        }

        if (!hasAttributeMetadata)
        {
            return true;
        }

        if (string.Equals(autonumberPropertyValue, "False", StringComparison.OrdinalIgnoreCase)
            || string.Equals(autonumberPropertyValue, "0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static bool IsNullable(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return string.Equals(text, "True", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "Yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "Nullable", StringComparison.OrdinalIgnoreCase);
    }
}
