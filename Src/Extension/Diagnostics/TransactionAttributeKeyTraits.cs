using System;
using Artech.Genexus.Common.Parts;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Critério compartilhado de autonumeração de parte de chave (Wizard flat e leitor hierárquico B095).
/// </summary>
internal static class TransactionAttributeKeyTraits
{
    /// <summary>
    /// GeneXus: autonumeração só existe em PK de um único campo. Em chave composta,
    /// nenhuma parte pode ser autonumerada — a contagem decide sem consultar a propriedade.
    /// Evidência 2026-08-06: Teste (PK=3, Autonumber='False') e NotaFiscal (PK=1, Autonumber='True').
    /// </summary>
    public static bool IsAutonumber(TransactionAttribute item, int primaryKeyPartCount)
    {
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
    /// </summary>
    public static bool IsAutonumberCore(int primaryKeyPartCount, bool hasAttributeMetadata, string? autonumberPropertyValue)
    {
        if (!hasAttributeMetadata)
        {
            return true;
        }

        if (primaryKeyPartCount > 1)
        {
            return false;
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
