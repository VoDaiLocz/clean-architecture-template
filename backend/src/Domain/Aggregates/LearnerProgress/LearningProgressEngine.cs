namespace Domain.Aggregates.LearnerProgress;

public sealed class LearningProgressEngine(LearningPathCatalog catalog)
{
    public UnitAccessResult GetUnitAccess(LearnerState state, string unitId)
    {
        var unit = catalog.GetUnit(unitId);
        var reasonCodes = new List<string>();

        if (unit.RequiredPreviousUnitId is not null && !state.CompletedUnitIds.Contains(unit.RequiredPreviousUnitId))
        {
            reasonCodes.Add("previous_unit_incomplete");
        }

        return new UnitAccessResult(reasonCodes.Count == 0, reasonCodes);
    }

    public void RecordLessonViewed(LearnerState state, string unitId)
    {
        _ = catalog.GetUnit(unitId);
        state.ViewedLessonUnitIds.Add(unitId);
    }

    public void RecordDrillCompleted(LearnerState state, string unitId, int correctCount, int totalCount)
    {
        _ = catalog.GetUnit(unitId);
        if (totalCount <= 0 || correctCount < totalCount)
        {
            return;
        }

        state.CompletedDrillUnitIds.Add(unitId);
    }

    public MiniTestAttemptResult RecordMiniTestAttempt(
        LearnerState state,
        string unitId,
        int correctCount,
        int totalCount,
        IReadOnlyList<string> wrongItemIds,
        string errorTag
    )
    {
        var unit = catalog.GetUnit(unitId);
        var scorePercent = totalCount <= 0 ? 0 : (int)Math.Round((correctCount * 100m) / totalCount);
        var createdReviewItemIds = CreateReviewItems(state, unitId, wrongItemIds, errorTag);
        var hasBlockingReview = state.ReviewQueue.Any(item => item.UnitId == unitId && !item.Resolved);
        var unitCompleted =
            state.ViewedLessonUnitIds.Contains(unitId)
            && state.CompletedDrillUnitIds.Contains(unitId)
            && scorePercent >= unit.MiniTestThresholdPercent
            && !hasBlockingReview;

        if (unitCompleted)
        {
            state.CompletedUnitIds.Add(unitId);
            state.ActiveUnitId = NextUnitId(unitId) ?? state.ActiveUnitId;
        }

        return new MiniTestAttemptResult(scorePercent, unitCompleted, createdReviewItemIds);
    }

    public void RecordReviewCompleted(LearnerState state, string reviewItemId)
    {
        var reviewItem = state.ReviewQueue.Single(item => item.ReviewItemId == reviewItemId);
        var index = state.ReviewQueue.IndexOf(reviewItem);
        state.ReviewQueue[index] = reviewItem with { Resolved = true };
    }

    private IReadOnlyList<string> CreateReviewItems(
        LearnerState state,
        string unitId,
        IReadOnlyList<string> wrongItemIds,
        string errorTag
    )
    {
        var createdReviewItemIds = new List<string>();
        foreach (var wrongItemId in wrongItemIds)
        {
            var reviewItemId = $"{unitId}:{wrongItemId}:review";
            if (state.ReviewQueue.Any(item => item.ReviewItemId == reviewItemId))
            {
                continue;
            }

            state.ReviewQueue.Add(new ReviewItemState(reviewItemId, unitId, wrongItemId, errorTag, Resolved: false));
            createdReviewItemIds.Add(reviewItemId);
        }

        return createdReviewItemIds;
    }

    private string? NextUnitId(string unitId)
    {
        var units = catalog.Units.ToList();
        var index = units.FindIndex(unit => unit.UnitId == unitId);
        if (index < 0 || index + 1 >= units.Count)
        {
            return null;
        }

        return units[index + 1].UnitId;
    }
}
