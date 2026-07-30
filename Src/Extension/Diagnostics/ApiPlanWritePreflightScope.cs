#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public sealed class ApiPlanWritePreflightScope
{
    private ApiPlanWritePreflightScope(
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile)
    {
        RequireSdts = requireSdts;
        RequireProcedures = requireProcedures;
        RequireApiObject = requireApiObject;
        RequireMetadataFile = requireMetadataFile;
    }

    public bool RequireSdts { get; }

    public bool RequireProcedures { get; }

    public bool RequireApiObject { get; }

    public bool RequireMetadataFile { get; }

    public static ApiPlanWritePreflightScope FromRequirements(
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile)
    {
        return new ApiPlanWritePreflightScope(requireSdts, requireProcedures, requireApiObject, requireMetadataFile);
    }

    public static ApiPlanWritePreflightScope FromSelection(
        bool generateSdts,
        bool generateProcedures,
        bool generateApiObject,
        bool generateMetadata,
        bool applyList,
        bool applyBusinessComponent)
    {
        return new ApiPlanWritePreflightScope(
            generateSdts || generateProcedures || generateApiObject || generateMetadata || applyList || applyBusinessComponent,
            generateProcedures || generateApiObject || generateMetadata || applyList || applyBusinessComponent,
            generateApiObject || generateMetadata || applyList || applyBusinessComponent,
            generateMetadata);
    }

    public string[] SelectBlockedStageNames(IEnumerable<ApiPlanWritePreflightStageBlock> stages)
    {
        if (stages is null)
        {
            throw new ArgumentNullException(nameof(stages));
        }

        return stages
            .Where(stage => stage is not null)
            .Where(stage => Includes(stage.StageKind))
            .Where(stage => stage.IsBlocked)
            .Select(stage => stage.StageName)
            .ToArray();
    }

    public bool Includes(ApiPlanWritePreflightStageKind stageKind)
    {
        return stageKind switch
        {
            ApiPlanWritePreflightStageKind.Sdts => RequireSdts,
            ApiPlanWritePreflightStageKind.Procedures => RequireProcedures,
            ApiPlanWritePreflightStageKind.ApiObject => RequireApiObject,
            ApiPlanWritePreflightStageKind.MetadataFile => RequireMetadataFile,
            _ => false,
        };
    }
}

public sealed class ApiPlanWritePreflightStageBlock
{
    public ApiPlanWritePreflightStageBlock(ApiPlanWritePreflightStageKind stageKind, string stageName, bool isBlocked)
    {
        StageKind = stageKind;
        StageName = stageName ?? throw new ArgumentNullException(nameof(stageName));
        IsBlocked = isBlocked;
    }

    public ApiPlanWritePreflightStageKind StageKind { get; }

    public string StageName { get; }

    public bool IsBlocked { get; }
}

public enum ApiPlanWritePreflightStageKind
{
    Sdts,
    Procedures,
    ApiObject,
    MetadataFile,
}
