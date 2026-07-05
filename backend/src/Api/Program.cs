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
using Application.Features.Learner.Work;
using Application.Features.SourceExtraction;
using Application.Features.SourceManifests;
using Application.Features.SourceReview;
using Application.Features.SourceValidation;
using Application.Features.Learner.Review;
using Application.Features.Learner.Mastery;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearningItems;
using Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;
using Api.Endpoints;
using Domain.Constants;
using Serilog;
using Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

#pragma warning disable CS0618

using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddInfrastructureDependencies(builder.Configuration, builder.Environment.EnvironmentName);

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["JwtSettings:Secret"] ?? "super-secret-key-that-is-at-least-32-bytes-long";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "ToeicApi",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "ToeicApi",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret))
        };
    });
builder.Services.AddAuthorization(options => {
    options.AddPolicy(Policies.RequireAdminRole, policy => policy.RequireRole(Roles.Admin, Roles.SuperAdmin));
    options.AddPolicy(Policies.RequireOperatorRole, policy => policy.RequireRole(Roles.Operator, Roles.Admin, Roles.SuperAdmin));
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => 
        policy.WithOrigins("http://localhost:4200", "https://your-production-domain.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRateLimiter();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

var api = app.MapGroup("/api");
var learner = api.MapGroup("/learner");
var admin = api.MapGroup("/admin").RequireAuthorization(Policies.RequireOperatorRole);

app.MapHealthChecks("/api/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/api/health/ready");

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

admin.MapPost(
    "/source-assets/{assetId}/parse-answer-keys",
    Ok<ParseToeicAnswerKeysResult> (
        string assetId,
        IKnowledgeRepository repository,
        IAnswerKeyParser parser
    ) =>
    {
        var handler = new ParseToeicAnswerKeysHandler(repository, parser);
        return TypedResults.Ok(handler.Handle(new ParseToeicAnswerKeysCommand(assetId)));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/parse-transcripts",
    Ok<ParseToeicTranscriptsResult> (
        string assetId,
        IKnowledgeRepository repository,
        ITranscriptParser parser
    ) =>
    {
        var handler = new ParseToeicTranscriptsHandler(repository, parser);
        return TypedResults.Ok(handler.Handle(new ParseToeicTranscriptsCommand(assetId)));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/parse-reading-drafts",
    Ok<ParseToeicReadingDraftsResult> (
        string assetId,
        IKnowledgeRepository repository,
        IReadingDraftParser parser
    ) =>
    {
        var handler = new ParseToeicReadingDraftsHandler(repository, parser);
        return TypedResults.Ok(handler.Handle(new ParseToeicReadingDraftsCommand(assetId)));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/parse-listening-groups",
    Ok<ParseToeicListeningGroupsResult> (
        string assetId,
        IKnowledgeRepository repository,
        IListeningDraftParser parser
    ) =>
    {
        var handler = new ParseToeicListeningGroupsHandler(repository, parser);
        return TypedResults.Ok(handler.Handle(new ParseToeicListeningGroupsCommand(assetId)));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/validate-drafts",
    Ok<ValidateToeicDraftContentResult> (
        string assetId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ValidateToeicDraftContentHandler(repository);
        return TypedResults.Ok(handler.Handle(new ValidateToeicDraftContentCommand(assetId)));
    }
);

admin.MapPost(
    "/source-assets/{assetId}/review-and-publish",
    Ok<ReviewAndPublishToeicContentResult> (
        string assetId,
        ReviewAndPublishRequest request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ReviewAndPublishToeicContentHandler(repository);
        return TypedResults.Ok(handler.Handle(new ReviewAndPublishToeicContentCommand(assetId, request.LessonId, request.Decisions)));
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
    "/placement/score",
    Ok<ScorePlacementSessionResponse> (
        ScorePlacementSessionCommand request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ScorePlacementSessionHandler(repository);
        return TypedResults.Ok(handler.Handle(request));
    }
);

learner.MapPost(
    "/path/generate",
    Ok<GenerateLearningPathResponse> (
        GenerateLearningPathCommand request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new GenerateLearningPathHandler(repository);
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
    "/today",
    Ok<LearnerTodayPlanResponse> (
        string learnerId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new GetLearnerTodayPlanHandler(repository);
        return TypedResults.Ok(handler.Handle(new GetLearnerTodayPlanQuery(learnerId)));
    }
);

learner.MapPost(
    "/assignments/{assignmentId}/sessions/start",
    Ok<ActivitySessionResponse> (
        string assignmentId,
        string learnerId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ManageActivitySessionHandler(repository);
        return TypedResults.Ok(handler.Handle(new StartActivitySessionCommand(assignmentId, learnerId)));
    }
);

learner.MapGet(
    "/sessions/{sessionId}",
    Ok<ActivitySessionResponse> (
        string sessionId,
        string learnerId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ManageActivitySessionHandler(repository);
        return TypedResults.Ok(handler.Handle(new GetActivitySessionQuery(sessionId, learnerId)));
    }
);

learner.MapPost(
    "/sessions/{sessionId}/complete",
    Ok<ActivitySessionResponse> (
        string sessionId,
        string learnerId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ManageActivitySessionHandler(repository);
        return TypedResults.Ok(handler.Handle(new CompleteActivitySessionCommand(sessionId, learnerId)));
    }
);

learner.MapPost(
    "/sessions/{sessionId}/abandon",
    Ok<ActivitySessionResponse> (
        string sessionId,
        string learnerId,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ManageActivitySessionHandler(repository);
        return TypedResults.Ok(handler.Handle(new AbandonActivitySessionCommand(sessionId, learnerId)));
    }
);

learner.MapPost(
    "/sessions/{sessionId}/attempts",
    Ok<SubmitAttemptResponse> (
        string sessionId,
        string learnerId,
        SubmitAttemptCommand command,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new SubmitAttemptHandler(repository);
        return TypedResults.Ok(handler.Handle(command with { SessionId = sessionId, LearnerId = learnerId }));
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
    Results<Ok<GetLearnerReviewQueueResponse>, NotFound> (
        string learnerId,
        GetLearnerReviewQueueHandler handler
    ) =>
    {
        var response = handler.Handle(new GetLearnerReviewQueueQuery(learnerId));
        return TypedResults.Ok(response);
    }
);

learner.MapGet(
    "/units/{unitId}/mastery",
    Results<Ok<LearnerMasteryResponse>, NotFound, BadRequest<string>> (
        string learnerId,
        string unitId,
        IKnowledgeRepository repository
    ) =>
    {
        try
        {
            var handler = new Application.Features.Learner.Mastery.GetLearnerMasteryHandler(repository);
            return TypedResults.Ok(handler.Handle(new Application.Features.Learner.Mastery.GetLearnerMasteryQuery(learnerId, unitId)));
        }
        catch (ArgumentException e)
        {
            if (e.Message == "MASTERY_RECORD_NOT_FOUND" || e.Message == "UNIT_NOT_IN_PATH") return TypedResults.NotFound();
            return TypedResults.BadRequest(e.Message);
        }
    }
);

learner.MapPost(
    "/review/{reviewItemId}/resolve",
    Results<Ok<ResolveReviewItemResponse>, NotFound, BadRequest<string>> (
        string reviewItemId,
        [Microsoft.AspNetCore.Mvc.FromBody] ResolveReviewItemRequest request,
        ResolveReviewItemHandler handler
    ) =>
    {
        try
        {
            var response = handler.Handle(new ResolveReviewItemCommand(request.LearnerId, reviewItemId, request.Answer));
            return TypedResults.Ok(response);
        }
        catch (ArgumentException ex) when (ex.Message is "REVIEW_ITEM_NOT_FOUND" or "REVIEW_ITEM_NOT_OWNED")
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex) when (ex.Message == "REPAIR_NOT_PASSED")
        {
            return TypedResults.BadRequest("REPAIR_NOT_PASSED");
        }
        catch (ArgumentException ex) when (ex.Message == "ALREADY_RESOLVED")
        {
            return TypedResults.BadRequest("ALREADY_RESOLVED");
        }
    }
);

app.MapGet("/", () => Results.Redirect("/api/dashboard"));

app.Run();

#pragma warning restore CS0618

public sealed record ResolveReviewItemRequest(string LearnerId, string Answer);

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

public sealed record ReviewAndPublishRequest(string LessonId, IReadOnlyList<ReviewDecision> Decisions);
