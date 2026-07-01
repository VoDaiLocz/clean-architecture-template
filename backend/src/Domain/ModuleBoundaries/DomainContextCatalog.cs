namespace Domain.ModuleBoundaries;

public static class DomainContextCatalog
{
    public static readonly IReadOnlyList<string> All =
    [
        "ContentFactory",
        "LearningContent",
        "LearnerJourney",
        "AttemptReview",
        "AnalyticsOperations",
    ];
}
