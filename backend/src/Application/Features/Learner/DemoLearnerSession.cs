using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner;

[Obsolete("P0.2 legacy demo-only learner flow. Do not use for production learner APIs.", false)]
public sealed class DemoLearnerSession
{
    public const bool IsLegacyDemoOnly = true;
    public const string ReplacementPhase = "P4";

    private readonly LearningPathCatalog catalog = LearningPathCatalog.CreateDefault();
    private readonly LearningProgressEngine engine;
    private LearnerState state;

    public DemoLearnerSession()
    {
        engine = new LearningProgressEngine(catalog);
        state = LearnerState.Start("demo-learner", catalog);
    }

    public LearnerHomeResponse GetHome()
    {
        var activeUnit = catalog.GetUnit(state.ActiveUnitId);
        return new LearnerHomeResponse(
            "demo-learner",
            activeUnit.Part,
            activeUnit.UnitId,
            activeUnit.Title,
            DetermineNextActivity(),
            state.ReviewQueue.Count(item => !item.Resolved),
            BuildLockedNextUnit()
        );
    }

    public LearnerActivityResponse GetActivity(string activityId)
    {
        return activityId switch
        {
            "part5-word-form-lesson" => new LearnerActivityResponse(
                activityId,
                "part5-word-form",
                "ConceptLesson",
                "Word Form: chọn đúng từ loại",
                "Học cách nhìn vị trí chỗ trống để đoán cần danh từ, động từ, tính từ hay trạng từ.",
                [
                    "Sau mạo từ hoặc tính từ thường cần danh từ.",
                    "Sau động từ thường cần trạng từ nếu đang bổ nghĩa cho hành động.",
                    "Trước danh từ thường cần tính từ."
                ],
                null
            ),
            "part5-word-form-drill" => new LearnerActivityResponse(
                activityId,
                "part5-word-form",
                "FocusDrill",
                "Drill: Word Form",
                "Làm 15 câu chỉ tập trung vào word form.",
                [],
                new LearnerQuestionResponse(
                    "p5-word-form-001",
                    "The marketing team needs a more ____ strategy for the new product.",
                    new Dictionary<string, string>
                    {
                        ["A"] = "effect",
                        ["B"] = "effective",
                        ["C"] = "effectively",
                        ["D"] = "effectiveness",
                    },
                    "B",
                    "Before the noun strategy, the blank needs an adjective: effective."
                )
            ),
            "part5-word-form-mini-test" => new LearnerActivityResponse(
                activityId,
                "part5-word-form",
                "MiniTest",
                "Mini test: Word Form",
                "Đạt 80% và sửa hết câu sai để mở khóa Verb Tense.",
                [],
                new LearnerQuestionResponse(
                    "p5-word-form-007",
                    "The supervisor reviewed the report ____ before the meeting.",
                    new Dictionary<string, string>
                    {
                        ["A"] = "careful",
                        ["B"] = "care",
                        ["C"] = "carefully",
                        ["D"] = "carefulness",
                    },
                    "C",
                    "The blank modifies reviewed, so it needs the adverb carefully."
                )
            ),
            _ => throw new ArgumentException($"Unknown activity: {activityId}", nameof(activityId)),
        };
    }

    public AttemptResponse SubmitActivityAttempt(string activityId, LearnerAttemptRequest request)
    {
        return activityId switch
        {
            "part5-word-form-lesson" => CompleteLesson(),
            "part5-word-form-drill" => CompleteDrill(request),
            "part5-word-form-mini-test" => CompleteMiniTest(request),
            _ => throw new ArgumentException($"Unknown activity: {activityId}", nameof(activityId)),
        };
    }

    public IReadOnlyList<LearnerReviewItemResponse> GetReview()
    {
        return state.ReviewQueue
            .Where(item => !item.Resolved)
            .Select(item => new LearnerReviewItemResponse(
                item.ReviewItemId,
                item.UnitId,
                item.QuestionId,
                item.ErrorTag,
                "Xem lại cách xác định từ loại trước khi làm lại câu sửa lỗi."
            ))
            .ToList();
    }

    public AttemptResponse SubmitReviewAttempt(string reviewItemId)
    {
        engine.RecordReviewCompleted(state, reviewItemId);
        return new AttemptResponse(
            ActivityCompleted: true,
            UnitCompleted: false,
            NextActivity: DetermineNextActivity(),
            ReviewCount: state.ReviewQueue.Count(item => !item.Resolved),
            Message: "Đã sửa lỗi. Làm lại mini test để mở khóa bài tiếp theo."
        );
    }

    public void Reset()
    {
        state = LearnerState.Start("demo-learner", catalog);
    }

    private AttemptResponse CompleteLesson()
    {
        engine.RecordLessonViewed(state, "part5-word-form");
        return Completed("Đã học lesson. Tiếp theo làm drill word form.");
    }

    private AttemptResponse CompleteDrill(LearnerAttemptRequest request)
    {
        engine.RecordDrillCompleted(
            state,
            "part5-word-form",
            request.CorrectCount ?? 15,
            request.TotalCount ?? 15
        );
        return Completed("Đã hoàn thành drill. Tiếp theo làm mini test.");
    }

    private AttemptResponse CompleteMiniTest(LearnerAttemptRequest request)
    {
        var result = engine.RecordMiniTestAttempt(
            state,
            "part5-word-form",
            request.CorrectCount ?? 0,
            request.TotalCount ?? 10,
            request.WrongItemIds ?? [],
            request.ErrorTag ?? "word_form"
        );

        return new AttemptResponse(
            ActivityCompleted: true,
            result.UnitCompleted,
            DetermineNextActivity(),
            state.ReviewQueue.Count(item => !item.Resolved),
            result.UnitCompleted
                ? "Đạt mastery. Verb Tense đã được mở khóa."
                : "Chưa đạt mastery. Cần sửa lỗi trước khi mở khóa bài tiếp theo."
        );
    }

    private AttemptResponse Completed(string message) =>
        new(
            ActivityCompleted: true,
            UnitCompleted: false,
            NextActivity: DetermineNextActivity(),
            ReviewCount: state.ReviewQueue.Count(item => !item.Resolved),
            Message: message
        );

    private LearnerActivitySummaryResponse DetermineNextActivity()
    {
        var unresolvedReview = state.ReviewQueue.FirstOrDefault(item => !item.Resolved);
        if (unresolvedReview is not null)
        {
            return new LearnerActivitySummaryResponse(
                $"review:{unresolvedReview.ReviewItemId}",
                "MistakeRepair",
                "Sửa lỗi word form",
                "Sửa lỗi còn lại để mở khóa bài tiếp theo."
            );
        }

        if (!state.ViewedLessonUnitIds.Contains("part5-word-form"))
        {
            return new LearnerActivitySummaryResponse(
                "part5-word-form-lesson",
                "ConceptLesson",
                "Học Word Form",
                "Học cách chọn đúng từ loại trước khi làm câu."
            );
        }

        if (!state.CompletedDrillUnitIds.Contains("part5-word-form"))
        {
            return new LearnerActivitySummaryResponse(
                "part5-word-form-drill",
                "FocusDrill",
                "Drill Word Form",
                "Luyện tập trung 15 câu word form."
            );
        }

        if (!state.CompletedUnitIds.Contains("part5-word-form"))
        {
            return new LearnerActivitySummaryResponse(
                "part5-word-form-mini-test",
                "MiniTest",
                "Mini test Word Form",
                "Đạt 80% để mở khóa Verb Tense."
            );
        }

        return new LearnerActivitySummaryResponse(
            "part5-verb-tense-lesson",
            "ConceptLesson",
            "Học Verb Tense",
            "Bài tiếp theo đã được mở khóa."
        );
    }

    private LockedUnitResponse? BuildLockedNextUnit()
    {
        var access = engine.GetUnitAccess(state, "part5-verb-tense");
        if (access.CanStart)
        {
            return null;
        }

        return new LockedUnitResponse(
            "part5-verb-tense",
            "Verb Tense",
            access.ReasonCodes,
            "Hoàn thành 100% Word Form để mở khóa."
        );
    }
}

public sealed record LearnerHomeResponse(
    string LearnerId,
    int CurrentPart,
    string CurrentUnitId,
    string CurrentUnitTitle,
    LearnerActivitySummaryResponse NextActivity,
    int ReviewCount,
    LockedUnitResponse? LockedNextUnit
);

public sealed record LearnerActivitySummaryResponse(
    string ActivityId,
    string ActivityType,
    string Title,
    string Description
);

public sealed record LockedUnitResponse(
    string UnitId,
    string Title,
    IReadOnlyList<string> ReasonCodes,
    string LearnerMessage
);

public sealed record LearnerActivityResponse(
    string ActivityId,
    string UnitId,
    string ActivityType,
    string Title,
    string Instructions,
    IReadOnlyList<string> LessonPoints,
    LearnerQuestionResponse? Question
);

public sealed record LearnerQuestionResponse(
    string QuestionId,
    string Prompt,
    IReadOnlyDictionary<string, string> Options,
    string CorrectAnswer,
    string Explanation
);

public sealed record LearnerAttemptRequest(
    int? CorrectCount,
    int? TotalCount,
    IReadOnlyList<string>? WrongItemIds,
    string? ErrorTag
);

public sealed record AttemptResponse(
    bool ActivityCompleted,
    bool UnitCompleted,
    LearnerActivitySummaryResponse NextActivity,
    int ReviewCount,
    string Message
);

public sealed record LearnerReviewItemResponse(
    string ReviewItemId,
    string UnitId,
    string QuestionId,
    string ErrorTag,
    string RepairPrompt
);
