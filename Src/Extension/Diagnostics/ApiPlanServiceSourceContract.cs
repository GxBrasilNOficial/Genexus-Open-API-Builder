using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public static class ApiPlanServiceSourceContract
{
    public static bool MatchesB054(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, Array.Empty<string>(), false, Array.Empty<string>(), false);
    }

    public static bool MatchesB055(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, true, Array.Empty<string>(), false);
    }

    public static bool MatchesB070(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        IEnumerable<string> listFilterNames,
        bool includeBusinessComponentParameters)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, includeBusinessComponentParameters, listFilterNames, true);
    }

    public static bool MatchesB079(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        IEnumerable<string> listFilterNames,
        bool hasListContract)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, true, listFilterNames, hasListContract, true, true);
    }

    public static bool MatchesB079InternalErrorOnly(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        IEnumerable<string> listFilterNames,
        bool hasListContract)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, true, listFilterNames, hasListContract, true, false);
    }

    public static bool MatchesPreviousB079RestMethodContract(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        IEnumerable<string> listFilterNames,
        bool hasListContract)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, true, listFilterNames, hasListContract, true, true, validateRestMethods: false);
    }

    private static bool Matches(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        bool includeBusinessComponentParameters,
        IEnumerable<string> listFilterNames,
        bool hasListContract)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, includeBusinessComponentParameters, listFilterNames, hasListContract, false);
    }

    private static bool Matches(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        bool includeBusinessComponentParameters,
        IEnumerable<string> listFilterNames,
        bool hasListContract,
        bool hasRestRuntimeContract)
    {
        return Matches(source, apiName, transactionName, moduleTarget, services, primaryKeyNames, includeBusinessComponentParameters, listFilterNames, hasListContract, hasRestRuntimeContract, false);
    }

    private static bool Matches(
        string source,
        string apiName,
        string transactionName,
        string moduleTarget,
        IEnumerable<string> services,
        IEnumerable<string> primaryKeyNames,
        bool includeBusinessComponentParameters,
        IEnumerable<string> listFilterNames,
        bool hasListContract,
        bool hasRestRuntimeContract,
        bool exposeErrorResponse,
        bool validateRestMethods = true)
    {
        if (string.IsNullOrWhiteSpace(source) ||
            string.IsNullOrWhiteSpace(apiName) ||
            string.IsNullOrWhiteSpace(transactionName) ||
            services is null ||
            primaryKeyNames is null)
        {
            return false;
        }

        var normalizedServices = services.ToArray();
        var normalizedPrimaryKeyNames = primaryKeyNames.ToArray();
        var normalizedListFilterNames = listFilterNames.ToArray();
        var compactSource = RemoveWhitespace(source);
        if (!compactSource.StartsWith(apiName + "{", StringComparison.Ordinal) ||
            CountOccurrences(compactSource, "=>") != normalizedServices.Length)
        {
            return false;
        }

        if (hasRestRuntimeContract &&
            validateRestMethods &&
            normalizedServices.Any(service => string.Equals(service, "Create", StringComparison.OrdinalIgnoreCase)) &&
            compactSource.IndexOf("[RestMethod(POST)]", StringComparison.Ordinal) < 0)
        {
            return false;
        }

        if (hasRestRuntimeContract &&
            validateRestMethods &&
            normalizedServices.Any(service => string.Equals(service, "Update", StringComparison.OrdinalIgnoreCase)) &&
            compactSource.IndexOf("[RestMethod(PUT)]", StringComparison.Ordinal) < 0)
        {
            return false;
        }

        if (hasRestRuntimeContract &&
            validateRestMethods &&
            normalizedPrimaryKeyNames.Length > 0 &&
            normalizedServices.Any(service => string.Equals(service, "Get", StringComparison.OrdinalIgnoreCase) || string.Equals(service, "Update", StringComparison.OrdinalIgnoreCase)) &&
            !normalizedPrimaryKeyNames.All(name => compactSource.IndexOf("{&" + name + "}", StringComparison.Ordinal) >= 0))
        {
            return false;
        }

        return normalizedServices.All(service =>
        {
            var servicePrefix = ServiceSignature(service, normalizedPrimaryKeyNames, includeBusinessComponentParameters, normalizedListFilterNames, hasListContract, hasRestRuntimeContract, exposeErrorResponse) + "=>";
            if (!TryReadProcedureCall(compactSource, servicePrefix, out var calledObject, out var calledArguments))
            {
                return false;
            }

            return string.Equals(calledObject, ExpectedProcedureReference(moduleTarget, transactionName, service), StringComparison.Ordinal) &&
                string.Equals(calledArguments, ProcedureArguments(service, normalizedPrimaryKeyNames, includeBusinessComponentParameters, normalizedListFilterNames, hasListContract, hasRestRuntimeContract), StringComparison.Ordinal);
        });
    }

    private static string ServiceSignature(
        string service,
        IReadOnlyList<string> primaryKeyNames,
        bool includeBusinessComponentParameters,
        IReadOnlyList<string> listFilterNames,
        bool hasListContract,
        bool hasRestRuntimeContract,
        bool exposeErrorResponse)
    {
        if (hasListContract && string.Equals(service, "List", StringComparison.OrdinalIgnoreCase))
        {
            return "List(" + string.Join(",", new[] { "in:&ApiPage", "in:&ApiPageSize" }.Concat(listFilterNames.Select(name => "in:&" + name)).Concat(new[] { "out:&ListResponse" })) + ")";
        }

        if (hasRestRuntimeContract && string.Equals(service, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return "Get(" + string.Join(",", primaryKeyNames.Select(name => "in:&" + name).Concat(exposeErrorResponse ? new[] { "out:&GetResponse", "out:&ErrorResponse" } : new[] { "out:&GetResponse" })) + ")";
        }

        if (!includeBusinessComponentParameters || (!string.Equals(service, "Create", StringComparison.OrdinalIgnoreCase) && !string.Equals(service, "Update", StringComparison.OrdinalIgnoreCase)))
        {
            return service + "()";
        }

        if (string.Equals(service, "Create", StringComparison.OrdinalIgnoreCase))
        {
            if (hasRestRuntimeContract && exposeErrorResponse)
            {
                return "Create(in:&CreateRequest,out:&CreateResponse,out:&ErrorResponse)";
            }

            return "Create(in:&CreateRequest,out:&CreateResponse)";
        }

        return "Update(" + string.Join(",", primaryKeyNames.Select(name => "in:&" + name).Concat(hasRestRuntimeContract && exposeErrorResponse ? new[] { "in:&UpdateRequest", "out:&UpdateResponse", "out:&ErrorResponse" } : new[] { "in:&UpdateRequest", "out:&UpdateResponse" })) + ")";
    }

    private static string ProcedureArguments(
        string service,
        IReadOnlyList<string> primaryKeyNames,
        bool includeBusinessComponentParameters,
        IReadOnlyList<string> listFilterNames,
        bool hasListContract,
        bool hasRestRuntimeContract)
    {
        if (hasListContract && string.Equals(service, "List", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(",", new[] { "&ApiPage", "&ApiPageSize" }.Concat(listFilterNames.Select(name => "&" + name)).Concat(new[] { "&ListResponse" }));
        }

        if (hasRestRuntimeContract && string.Equals(service, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(",", primaryKeyNames.Select(name => "&" + name).Concat(new[] { "&GetResponse", "&ErrorResponse", "&RestStatusCode" }));
        }

        if (!includeBusinessComponentParameters)
        {
            return string.Empty;
        }

        if (string.Equals(service, "Create", StringComparison.OrdinalIgnoreCase))
        {
            if (hasRestRuntimeContract)
            {
                return "&CreateRequest,&CreateResponse,&ErrorResponse,&RestStatusCode";
            }

            return "&CreateRequest,&CreateResponse";
        }

        if (string.Equals(service, "Update", StringComparison.OrdinalIgnoreCase))
        {
            if (hasRestRuntimeContract)
            {
                return string.Join(",", primaryKeyNames.Select(name => "&" + name).Concat(new[] { "&UpdateRequest", "&UpdateResponse", "&ErrorResponse", "&RestStatusCode" }));
            }

            return string.Join(",", primaryKeyNames.Select(name => "&" + name).Concat(new[] { "&UpdateRequest", "&UpdateResponse" }));
        }

        return string.Empty;
    }

    private static string ExpectedProcedureReference(string moduleTarget, string transactionName, string service)
    {
        var procedure = "proc" + transactionName + "_API_" + service;
        return string.IsNullOrWhiteSpace(moduleTarget) || string.Equals(moduleTarget, "Root Module", StringComparison.Ordinal)
            ? procedure
            : moduleTarget + "." + procedure;
    }

    private static bool TryReadProcedureCall(string source, string servicePrefix, out string calledObject, out string calledArguments)
    {
        calledObject = string.Empty;
        calledArguments = string.Empty;
        var serviceIndex = source.IndexOf(servicePrefix, StringComparison.Ordinal);
        if (serviceIndex < 0)
        {
            return false;
        }

        var callStart = serviceIndex + servicePrefix.Length;
        var argumentsStart = source.IndexOf("(", callStart, StringComparison.Ordinal);
        var argumentsEnd = argumentsStart < 0 ? -1 : source.IndexOf(");", argumentsStart, StringComparison.Ordinal);
        if (argumentsStart < 0 || argumentsEnd < 0)
        {
            return false;
        }

        calledObject = source.Substring(callStart, argumentsStart - callStart);
        calledArguments = source.Substring(argumentsStart + 1, argumentsEnd - argumentsStart - 1);
        return true;
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static string RemoveWhitespace(string value) => new(value.Where(character => !char.IsWhiteSpace(character)).ToArray());
}
