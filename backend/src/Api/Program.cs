using Application;
using Application.Common.Interfaces.Repositories;
using Application.Features.Dashboard.Queries;
using Application.Features.LearningItems.Commands;
using Domain.Aggregates.LearningItems;
using Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddInfrastructureDependencies(builder.Configuration);
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

api.MapGet(
    "/dashboard",
    Ok<DashboardResponse> (IKnowledgeRepository repository) =>
    {
        var handler = new GetDashboardHandler(repository);
        return TypedResults.Ok(handler.Handle());
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

app.MapGet("/", () => Results.Redirect("/api/dashboard"));

app.Run();

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
