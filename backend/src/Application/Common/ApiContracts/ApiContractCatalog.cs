namespace Application.Common.ApiContracts;

public static class ApiContractCatalog
{
    public const string Version = "2026-07-01";

    public static readonly IReadOnlyList<ApiContractDescriptor> All =
    [
        new("GET", "/api/health", ApiAudience.Operations, "PlatformHealthSnapshot"),
        new("GET", "/api/dashboard", ApiAudience.Admin, "DashboardResponse"),
        new("POST", "/api/source-manifest/toeic-audit", ApiAudience.Admin, "ImportToeicSourceManifestResult"),
        new("GET", "/api/source-manifest/summary", ApiAudience.Admin, "SourceManifestSummary"),
        new("POST", "/api/raw-sources", ApiAudience.Admin, "RawSourceResponse"),
        new("POST", "/api/learning-items", ApiAudience.Admin, "PublishLearningItemResponse"),
        new("POST", "/api/learner/demo/reset", ApiAudience.LegacyDemo, "NoContent"),
        new("POST", "/api/learner/onboarding", ApiAudience.Learner, "OnboardLearnerResponse"),
        new("POST", "/api/learner/placement/start", ApiAudience.Learner, "StartPlacementSessionResponse"),
        new("GET", "/api/learner/home", ApiAudience.Learner, "LearnerHomeResponse"),
        new("GET", "/api/learner/activities/{activityId}", ApiAudience.Learner, "LearnerActivityResponse"),
        new("POST", "/api/learner/activities/{activityId}/attempts", ApiAudience.Learner, "AttemptResponse"),
        new("GET", "/api/learner/review", ApiAudience.Learner, "LearnerReviewItemResponse[]"),
        new("POST", "/api/learner/review/{reviewItemId}/attempts", ApiAudience.Learner, "AttemptResponse"),
    ];
}

public sealed record ApiContractDescriptor(
    string Method,
    string Route,
    ApiAudience Audience,
    string ResponseContract
);

public enum ApiAudience
{
    Learner,
    Admin,
    Operations,
    LegacyDemo,
}
