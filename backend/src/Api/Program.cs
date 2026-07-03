using Application;
using Application.Common.Health;
using Application.Common.Interfaces.Repositories;
using Application.Features.ContentCoverage;
using Application.Features.Dashboard.Queries;
using Application.Features.LearningItems.Commands;
using Application.Features.Learner;
using Application.Features.Learner.Home;
using Application.Features.Learner.Onboarding;
using Application.Features.Learner.Placement;
using Application.Features.SourceExtraction;
using Application.Features.SourceManifests;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearningItems;
using Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;

#pragma warning disable CS0618

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddInfrastructureDependencies(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "frontend",
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseCors("frontend");

var api = app.MapGroup("/api");
var learner = api.MapGroup("/learner");
var admin = api.MapGroup("/admin");

api.MapGet(
    "/health",
    Ok<PlatformHealthSnapshot> (IPlatformHealthService healthService) =>
    {
        return TypedResults.Ok(healthService.Check());
    }
);

api.MapGet(
    "/dashboard",
    Ok<DashboardResponse> (IKnowledgeRepository repository) =>
    {
        var handler = new GetDashboardHandler(repository);
        return TypedResults.Ok(handler.Handle());
    }
);

admin.MapGet(
    "/content-coverage",
    Ok<ContentCoverageSnapshot> (IKnowledgeRepository repository) =>
    {
        var handler = new GetContentCoverageHandler(repository);
        return TypedResults.Ok(handler.Handle());
    }
);

api.MapPost(
    "/source-manifest/toeic-audit",
    Ok<ImportToeicSourceManifestResult> (IKnowledgeRepository repository) =>
    {
        var handler = new ImportToeicSourceManifestHandler(repository);
        return TypedResults.Ok(handler.Handle());
    }
);

api.MapGet(
    "/source-manifest/summary",
    Ok<SourceManifestSummary> (IKnowledgeRepository repository) =>
    {
        var handler = new GetSourceManifestSummaryHandler(repository);
        return TypedResults.Ok(handler.Handle());
    }
);

api.MapGet(
    "/admin/duplicate-assets",
    Ok<DuplicateAssetReport> (IKnowledgeRepository repository) =>
    {
        var handler = new GetDuplicateAssetsHandler(repository);
        return TypedResults.Ok(handler.Handle());
    }
);

api.MapGet(
    "/admin/rejected-files",
    Ok<RejectedFilesReport> (IKnowledgeRepository repository) =>
    {
        var handler = new GetRejectedFilesHandler(repository);
        return TypedResults.Ok(handler.Handle());
    }
);

api.MapPost(
    "/source-manifest/local-downloads",
    Ok<ImportLocalToeicDownloadsResult> (
        ImportLocalToeicDownloadsCommand request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ImportLocalToeicDownloadsHandler(repository);
        return TypedResults.Ok(handler.Handle(request));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/extract-blocks",
    Ok<ExtractToeicPdfBlocksResult> (
        string assetId,
        IKnowledgeRepository repository,
        IPdfTextBlockExtractor extractor
    ) =>
    {
        var handler = new ExtractToeicPdfBlocksHandler(repository, extractor);
        return TypedResults.Ok(handler.Handle(new ExtractToeicPdfBlocksCommand(assetId)));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/extract-audio-metadata",
    Ok<ExtractToeicAudioMetadataResult> (
        string assetId,
        IKnowledgeRepository repository,
        IAudioMetadataProbe probe
    ) =>
    {
        var handler = new ExtractToeicAudioMetadataHandler(repository, probe);
        return TypedResults.Ok(handler.Handle(new ExtractToeicAudioMetadataCommand(assetId)));
    }
);

api.MapPost(
    "/raw-sources",
    Created<RawSourceResponse> (
        RawSourceRequest request,
        IKnowledgeRepository repository
    ) =>
    {
        repository.InsertRawSource(request.SourceId, request.Title, request.Url, request.Status);
        return TypedResults.Created($"/api/raw-sources/{request.SourceId}", new RawSourceResponse(request.SourceId));
    }
);

api.MapPost(
    "/learning-items",
    Results<Ok<PublishLearningItemResponse>, BadRequest<PublishLearningItemResponse>> (
        PublishLearningItemRequest request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new PublishLearningItemHandler(repository);
        var response = handler.Handle(new PublishLearningItemCommand(request.ToDraftLearningItem()));

        return response.CanPublish ? TypedResults.Ok(response) : TypedResults.BadRequest(response);
    }
);

learner.MapPost(
    "/onboarding",
    Ok<OnboardLearnerResponse> (
        OnboardLearnerCommand request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new OnboardLearnerHandler(repository);
        return TypedResults.Ok(handler.Handle(request));
    }
);

learner.MapPost(
    "/placement/start",
    Ok<StartPlacementSessionResponse> (
        StartPlacementSessionCommand request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new StartPlacementSessionHandler(repository);
        return TypedResults.Ok(handler.Handle(request));
    }
);

learner.MapPost(
    "/demo/reset",
    NoContent (DemoLearnerSession session) =>
    {
        session.Reset();
        return TypedResults.NoContent();
    }
);

learner.MapGet(
    "/home",
    Ok<LearnerHomeResponse> (
        string? learnerId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new GetLearnerHomeHandler(repository);
        return TypedResults.Ok(handler.Handle(new GetLearnerHomeQuery(learnerId ?? "demo-learner")));
    }
);

learner.MapGet(
    "/activities/{activityId}",
    Results<Ok<LearnerActivityResponse>, NotFound> (
        string activityId,
        DemoLearnerSession session
    ) =>
    {
        try
        {
            return TypedResults.Ok(session.GetActivity(activityId));
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
    }
);

learner.MapPost(
    "/activities/{activityId}/attempts",
    Results<Ok<AttemptResponse>, NotFound> (
        string activityId,
        LearnerAttemptRequest request,
        DemoLearnerSession session
    ) =>
    {
        try
        {
            return TypedResults.Ok(session.SubmitActivityAttempt(activityId, request));
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
    }
);

learner.MapGet(
    "/review",
    Ok<IReadOnlyList<LearnerReviewItemResponse>> (DemoLearnerSession session) =>
    {
        return TypedResults.Ok(session.GetReview());
    }
);

learner.MapPost(
    "/review/{reviewItemId}/attempts",
    Results<Ok<AttemptResponse>, NotFound> (
        string reviewItemId,
        DemoLearnerSession session
    ) =>
    {
        try
        {
            return TypedResults.Ok(session.SubmitReviewAttempt(reviewItemId));
        }
        catch (InvalidOperationException)
        {
            return TypedResults.NotFound();
        }
    }
);

app.MapGet("/", () => Results.Redirect("/api/dashboard"));

app.Run();

#pragma warning restore CS0618

public sealed record RawSourceRequest(
    string SourceId,
    string Title,
    string Url,
    string Status
);

public sealed record RawSourceResponse(string SourceId);

public sealed record PublishLearningItemRequest(
    LearningItemType ItemType,
    ToeicSkill Skill,
    int? Part,
    string Prompt,
    Dictionary<string, string> Options,
    string CorrectAnswer,
    string Explanation,
    SourceRefRequest? SourceRef,
    decimal Confidence,
    string? GroupRef,
    string Word,
    string Meaning
)
{
    public DraftLearningItem ToDraftLearningItem() =>
        new(
            ItemType,
            Skill,
            Part,
            Prompt,
            Options,
            CorrectAnswer,
            Explanation,
            SourceRef?.ToSourceRef(),
            Confidence,
            GroupRef,
            Word,
            Meaning
        );
}

public sealed record SourceRefRequest(
    string SourceId,
    string FileId,
    int? Page,
    string? BlockId
)
{
    public SourceRef ToSourceRef() => new(SourceId, FileId, Page, BlockId);
}
