#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Types;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B112 — sonda temporária para separar mutação entre o Pump e o Save de uma falha
/// intrínseca de validação do objeto GeneXus.
///
/// A sonda registra apenas fingerprints do estado em memória. Não altera Source, Rules,
/// variáveis, ordem dos Saves ou qualquer decisão do fluxo. O fingerprint inclui tamanho e
/// SHA-256 de Source/Rules, assinatura na ordem enumerada das variáveis e propriedades expandidas das
/// variáveis relevantes para o caso B109.
/// </summary>
internal static class B112SaveBoundaryProbe
{
    [ThreadStatic]
    private static B112SaveBoundaryLog? _current;

    public static IDisposable Begin(B112SaveBoundaryLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var previous = _current;
        _current = log;
        return new Scope(previous);
    }

    public static void PumpBoundary(string stage, string label, string before, string after) =>
        _current?.PumpBoundary(stage, label, before, after);

    public static void BeforeSave(string stage, string label, string snapshot) =>
        _current?.BeforeSave(stage, label, snapshot);

    public static void PreparedProcedure(string stage, Procedure procedure, IEnumerable<string> expectedVariables) =>
        _current?.PreparedProcedure(stage, procedure, expectedVariables);

    public static void PreparedApi(string stage, API api) =>
        _current?.PreparedApi(stage, api);

    public static void Saved(string stage, string label, string snapshot) =>
        _current?.Saved(stage, label, snapshot);

    public static void Failed(string stage, string label, Exception exception, string snapshot) =>
        _current?.Failed(stage, label, exception, snapshot);

    public static string Snapshot(Procedure procedure)
    {
        try
        {
            var variables = procedure.Variables.Variables
                .Where(variable => !variable.IsStandard)
                .Select(DescribeVariable)
                .ToArray();
            var variableSignature = string.Join("\u001f", variables);
            var focus = variables
                .Where(item => item.StartsWith("&HttpResponse:", StringComparison.OrdinalIgnoreCase) ||
                               item.StartsWith("&LocationUrl:", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return DescribeObject(
                "Procedure",
                procedure.Name,
                procedure.Guid,
                procedure.Rules.Source,
                procedure.ProcedurePart.Source,
                variables.Length,
                variableSignature,
                focus);
        }
        catch (Exception exception)
        {
            return "SnapshotError=Procedure:" + exception.GetType().FullName + ":" + Flatten(exception.Message);
        }
    }

    public static string Snapshot(API api)
    {
        try
        {
            var variables = api.Variables.Variables
                .Where(variable => !variable.IsStandard)
                .Select(DescribeVariable)
                .ToArray();
            var variableSignature = string.Join("\u001f", variables);

            return DescribeObject(
                "API",
                api.Name,
                api.Guid,
                api.Events.Source,
                api.ServiceGroupSource.Source,
                variables.Length,
                variableSignature,
                Array.Empty<string>());
        }
        catch (Exception exception)
        {
            return "SnapshotError=API:" + exception.GetType().FullName + ":" + Flatten(exception.Message);
        }
    }

    private static string DescribeObject(
        string kind,
        string name,
        Guid guid,
        string? rules,
        string? source,
        int variableCount,
        string variableSignature,
        IReadOnlyList<string> focus)
    {
        rules ??= string.Empty;
        source ??= string.Empty;
        var focusText = focus.Count == 0 ? "<none>" : string.Join(" || ", focus);
        return string.Format(
            CultureInfo.InvariantCulture,
            "Kind={0};Name={1};Guid={2};RulesChars={3};RulesSha256={4};SourceChars={5};SourceSha256={6};Vars={7};VarsSha256={8};Focus={9}",
            kind,
            Clean(name),
            guid,
            rules.Length,
            Sha256(rules),
            source.Length,
            Sha256(source),
            variableCount,
            Sha256(variableSignature),
            Clean(focusText));
    }

    private static string DescribeVariable(object variable)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "&{0}:Type={1};ATTCUSTOMTYPE={2};Length={3};Decimals={4};KBObject={5};Domain={6};Attribute={7}",
            Clean(ReadPublicProperty(variable, "Name")),
            Clean(ReadPublicProperty(variable, "Type")),
            Clean(ReadCustomType(variable)),
            Clean(ReadPublicProperty(variable, "Length")),
            Clean(ReadPublicProperty(variable, "Decimals")),
            Clean(ReadNestedName(variable, "KBObject")),
            Clean(ReadNestedName(variable, "DomainBasedOn")),
            Clean(ReadNestedName(variable, "AttributeBasedOn")));
    }

    private static string ReadCustomType(object instance)
    {
        try
        {
            var method = instance.GetType().GetMethod("GetPropertyValueString", new[] { typeof(string) });
            if (method is null)
            {
                return "<nao-exposto>";
            }

            return Convert.ToString(method.Invoke(instance, new object[] { "ATTCUSTOMTYPE" }), CultureInfo.InvariantCulture) ?? "<null>";
        }
        catch (Exception exception)
        {
            return "<erro:" + exception.GetType().Name + ">";
        }
    }

    private static string ReadPublicProperty(object instance, string propertyName)
    {
        try
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property is null)
            {
                return "<nao-exposto>";
            }

            return Convert.ToString(property.GetValue(instance, null), CultureInfo.InvariantCulture) ?? "<null>";
        }
        catch (Exception exception)
        {
            return "<erro:" + exception.GetType().Name + ">";
        }
    }

    private static string ReadNestedName(object instance, string propertyName)
    {
        try
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var nested = property?.GetValue(instance, null);
            return nested is null ? "<null>" : ReadPublicProperty(nested, "Name");
        }
        catch (Exception exception)
        {
            return "<erro:" + exception.GetType().Name + ">";
        }
    }

    private static string Sha256(string value)
    {
        using (var sha = SHA256.Create())
        {
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "<empty>";
        }

        return value!.Replace("\r", " ").Replace("\n", " ");
    }

    private static string Flatten(string? value) => Clean(value);

    private sealed class Scope : IDisposable
    {
        private readonly B112SaveBoundaryLog? _previous;
        private bool _disposed;

        public Scope(B112SaveBoundaryLog? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _current = _previous;
        }
    }
}

internal sealed class B112SaveBoundaryLog
{
    private readonly List<string> _lines = new List<string>();

    public void PumpBoundary(string stage, string label, string before, string after)
    {
        var changed = !string.Equals(before, after, StringComparison.Ordinal);
        _lines.Add(
            $"PumpBoundary Stage='{Clean(stage)}' Step='{Clean(label)}' Changed={changed} Before=[{before}] After=[{after}]");
    }

    public void BeforeSave(string stage, string label, string snapshot) =>
        _lines.Add($"BeforeSave Stage='{Clean(stage)}' Step='{Clean(label)}' State=[{snapshot}]");

    public void PreparedProcedure(string stage, Procedure procedure, IEnumerable<string> expectedVariables)
    {
        var expected = string.Join(" | ", expectedVariables ?? Enumerable.Empty<string>());
        _lines.Add(
            $"PreparedProcedure Stage='{Clean(stage)}' Step='{Clean(procedure.Name)}' Expected=[{expected}] State=[{B112SaveBoundaryProbe.Snapshot(procedure)}]");
    }

    public void PreparedApi(string stage, API api) =>
        _lines.Add($"PreparedApi Stage='{Clean(stage)}' Step='{Clean(api.Name)}' State=[{B112SaveBoundaryProbe.Snapshot(api)}]");

    public void Saved(string stage, string label, string snapshot) =>
        _lines.Add($"Saved Stage='{Clean(stage)}' Step='{Clean(label)}' State=[{snapshot}]");

    public void Failed(string stage, string label, Exception exception, string snapshot) =>
        _lines.Add(
            $"Failed Stage='{Clean(stage)}' Step='{Clean(label)}' ExceptionType='{exception.GetType().FullName}' ExceptionMessage='{Clean(exception.Message)}' State=[{snapshot}]");

    public IReadOnlyList<string> Render()
    {
        if (_lines.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string> { "== B112 — fronteiras Pump/Save ==" };
        result.AddRange(_lines);
        return result;
    }

    private static string Clean(string? value) =>
        string.IsNullOrEmpty(value) ? "<empty>" : value!.Replace("\r", " ").Replace("\n", " ");
}
