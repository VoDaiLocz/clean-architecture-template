using Application.Features.Dashboard.Queries;
using Application.Features.LearningItems.Commands;
using Application.Features.Learner;
using Application.Features.SourceManifests;
using Application.ModuleBoundaries;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Domain.ModuleBoundaries;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection;

var tests = new List<(string Name, Action Run)>
{
    ("valid Part 5 item publishes", ApplicationTests.ValidPart5ItemPublishes),
    ("invalid answer is rejected before persistence", ApplicationTests.InvalidAnswerIsRejectedBeforePersistence),
    ("low confidence goes to review issues", ApplicationTests.LowConfidenceGoesToReview),
    ("dashboard reports raw learning and issue counts", ApplicationTests.DashboardReportsCounts),
    ("dashboard reports corpus coverage without pretending backlog is published", ApplicationTests.DashboardReportsCorpusCoverage),
    ("source manifest classifier identifies provider material and access status", ApplicationTests.SourceManifestClassifierIdentifiesProviderMaterialAndAccessStatus),
    ("repository persists normalized source manifest entries", ApplicationTests.RepositoryPersistsNormalizedSourceManifestEntries),
    ("imports audited TOEIC source manifest into database", ApplicationTests.ImportsAuditedToeicSourceManifestIntoDatabase),
    ("dashboard includes normalized source manifest summary", ApplicationTests.DashboardIncludesNormalizedSourceManifestSummary),
    ("learner cannot unlock next unit until mastery gates pass", ApplicationTests.LearnerCannotUnlockNextUnitUntilMasteryGatesPass),
    ("demo learner session is marked legacy non-production", ApplicationTests.DemoLearnerSessionIsMarkedLegacyNonProduction),
    ("backend module boundaries are explicit", ApplicationTests.BackendModuleBoundariesAreExplicit),
    ("production configuration requires explicit database", ApplicationTests.ProductionConfigurationRequiresExplicitDatabase),
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} test(s) failed.");
    return 1;
}

Console.WriteLine($"{tests.Count} tests passed.");
return 0;

static class ApplicationTests
{
    public static void ValidPart5ItemPublishes()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var handler = new PublishLearningItemHandler(repository);

        var response = handler.Handle(new PublishLearningItemCommand(TestItems.ValidPart5Question()));

        Assert.True(response.CanPublish, "Valid item should publish.");
        Assert.Equal(1, repository.Count("learning_items"), "Expected one learning item.");
        Assert.Equal(0, repository.Count("validation_issues"), "Valid item should not create issues.");
    }

    public static void InvalidAnswerIsRejectedBeforePersistence()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var handler = new PublishLearningItemHandler(repository);
        var item = TestItems.ValidPart5Question() with { CorrectAnswer = "E" };

        var response = handler.Handle(new PublishLearningItemCommand(item));

        Assert.False(response.CanPublish, "Invalid answer must not publish.");
        Assert.Equal(0, repository.Count("learning_items"), "Invalid item must not reach learning table.");
        Assert.Equal(1, repository.Count("validation_issues"), "Validation issue should be stored.");
        Assert.Contains(response.IssueCodes, "answer_not_in_options");
    }

    public static void LowConfidenceGoesToReview()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var handler = new PublishLearningItemHandler(repository);
        var item = TestItems.ValidPart5Question() with { Confidence = 0.62m };

        var response = handler.Handle(new PublishLearningItemCommand(item));

        Assert.False(response.CanPublish, "Low-confidence item must not publish.");
        Assert.True(response.NeedsReview, "Low-confidence item should need review.");
        Assert.Contains(response.IssueCodes, "low_confidence");
    }

    public static void DashboardReportsCounts()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        repository.InsertRawSource("sheet-row-1", "Từ vựng Part 2", "https://drive.google.com/file/d/example/view", "opens");
        var publishHandler = new PublishLearningItemHandler(repository);
        publishHandler.Handle(new PublishLearningItemCommand(TestItems.ValidPart5Question()));
        publishHandler.Handle(new PublishLearningItemCommand(TestItems.ValidPart5Question() with { CorrectAnswer = "E" }));
        var dashboardHandler = new GetDashboardHandler(repository);

        var response = dashboardHandler.Handle();

        Assert.Equal(1, response.RawSourceCount, "Expected one raw source.");
        Assert.Equal(1, response.LearningItemCount, "Expected one learning item.");
        Assert.Equal(1, response.ValidationIssueCount, "Expected one validation issue.");
    }

    public static void DashboardReportsCorpusCoverage()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        repository.UpsertCorpusManifest(new CorpusManifest(
            CorpusId: "toeic-master",
            Title: "TOEIC Google Sheet + PDF book library",
            SheetTabs: 3,
            SheetRows: 18000,
            PdfBooks: 64,
            PdfPages: 12800,
            AudioFiles: 0,
            TargetLearningItems: 54000
        ));
        repository.UpsertNormalizationStage(new NormalizationStageSnapshot("inventory", "Inventory scan", 18867, 932, 0));
        repository.UpsertNormalizationStage(new NormalizationStageSnapshot("extraction", "Text extraction", 12800, 418, 21));
        var publishHandler = new PublishLearningItemHandler(repository);
        publishHandler.Handle(new PublishLearningItemCommand(TestItems.ValidPart5Question()));

        var response = new GetDashboardHandler(repository).Handle();

        Assert.Equal(3, response.Corpus.SheetTabs, "Expected imported sheet tab plan.");
        Assert.Equal(64, response.Corpus.PdfBooks, "Expected PDF book backlog.");
        Assert.Equal(54000, response.Corpus.TargetLearningItems, "Expected corpus-scale target.");
        Assert.Equal(1, response.LearningItemCount, "Backlog must not inflate published learning items.");
        Assert.True(response.NormalizationStages.Count >= 5, "Expected full stage coverage rows.");
        var inventoryStage = response.NormalizationStages.Single(stage => stage.StageKey == "inventory");
        Assert.Equal(932, inventoryStage.CompletedCount, "Expected completed inventory count.");
    }

    public static void SourceManifestClassifierIdentifiesProviderMaterialAndAccessStatus()
    {
        var driveFolder = SourceManifestClassifier.Classify(
            7,
            "SPARTA TOEIC ( quyển hồng - 10TEST )",
            "https://drive.google.com/drive/folders/1oHUHYyEQ0T5H-rl_fXHMjljV4lGKCRB-",
            inaccessible: false,
            hasPdf: true,
            hasAudio: true,
            hasTranscript: true,
            hasAnswerKey: true,
            hasImage: false
        );

        Assert.Equal(SourceProvider.GoogleDrive, driveFolder.Provider, "Expected Google Drive provider.");
        Assert.Equal(SourceType.DriveFolder, driveFolder.SourceType, "Expected Drive folder type.");
        Assert.Equal(MaterialClass.TestBook, driveFolder.PrimaryMaterialClass, "SPARTA is a test book.");
        Assert.Equal(SourceAccessStatus.Accessible, driveFolder.AccessStatus, "Expected accessible source.");
        Assert.True(driveFolder.Evidence.HasAudio, "Expected audio evidence.");
        Assert.True(driveFolder.Evidence.HasTranscript, "Expected transcript evidence.");
        Assert.True(driveFolder.Evidence.HasAnswerKey, "Expected answer key evidence.");

        var blockedGrammar = SourceManifestClassifier.Classify(
            62,
            "Advanced Grammar in Use",
            "https://drive.google.com/file/d/example/view",
            inaccessible: true,
            hasPdf: false,
            hasAudio: false,
            hasTranscript: false,
            hasAnswerKey: false,
            hasImage: false
        );

        Assert.Equal(SourceAccessStatus.AccessBlocked, blockedGrammar.AccessStatus, "Blocked source must be explicit.");
        Assert.Equal(MaterialClass.GrammarReference, blockedGrammar.PrimaryMaterialClass, "Grammar book should not become test bank.");
    }

    public static void RepositoryPersistsNormalizedSourceManifestEntries()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var entry = SourceManifestClassifier.Classify(
            7,
            "SPARTA TOEIC ( quyển hồng - 10TEST )",
            "https://drive.google.com/drive/folders/1oHUHYyEQ0T5H-rl_fXHMjljV4lGKCRB-",
            inaccessible: false,
            hasPdf: true,
            hasAudio: true,
            hasTranscript: true,
            hasAnswerKey: true,
            hasImage: false
        );

        repository.UpsertSourceManifestEntry(entry);

        Assert.Equal(1, repository.Count("source_manifest_entries"), "Expected one normalized source row.");
        var entries = repository.GetSourceManifestEntries();
        Assert.Equal(1, entries.Count, "Expected one source manifest entry.");
        Assert.Equal("sheet-row-7", entries.Single().SourceId, "Expected stable source id.");
        var summary = repository.GetSourceManifestSummary();
        Assert.Equal(1, summary.TotalSources, "Expected one total source.");
        Assert.Equal(1, summary.AccessibleSources, "Expected one accessible source.");
        Assert.Equal(1, summary.DriveFolders, "Expected one Drive folder.");
        Assert.Equal(1, summary.SourcesWithAudio, "Expected audio count.");
        Assert.Equal(1, summary.SourcesWithAnswerKey, "Expected answer-key count.");
    }

    public static void ImportsAuditedToeicSourceManifestIntoDatabase()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var handler = new ImportToeicSourceManifestHandler(repository);

        var result = handler.Handle();

        Assert.Equal(73, result.ImportedCount, "Expected all audited source rows imported.");
        Assert.Equal(13, result.BlockedCount, "Expected blocked source count from audit.");
        Assert.Equal(33, result.SourcesWithPdf, "Expected PDF evidence count from audit.");
        Assert.Equal(20, result.SourcesWithAudio, "Expected audio evidence count from audit.");
        Assert.Equal(6, result.SourcesWithTranscript, "Expected transcript evidence count from audit.");
        Assert.Equal(5, result.SourcesWithAnswerKey, "Expected answer-key evidence count from audit.");
        Assert.Equal(73, repository.Count("source_manifest_entries"), "Expected DB rows.");
        var summary = new GetSourceManifestSummaryHandler(repository).Handle();
        Assert.Equal(36, summary.DriveFolders, "Expected Drive folder count from audit.");
        Assert.Equal(14, summary.DriveFiles, "Expected Drive file count from audit.");
        Assert.Equal(4, summary.Shortlinks, "Expected shortlink count from audit.");
    }

    public static void DashboardIncludesNormalizedSourceManifestSummary()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        new ImportToeicSourceManifestHandler(repository).Handle();

        var response = new GetDashboardHandler(repository).Handle();

        Assert.Equal(73, response.SourceManifest.TotalSources, "Dashboard should show normalized source inventory.");
        Assert.Equal(13, response.SourceManifest.BlockedSources, "Dashboard should show blocked source count.");
        Assert.Equal(36, response.SourceManifest.DriveFolders, "Dashboard should show Drive folder count.");
    }

    public static void LearnerCannotUnlockNextUnitUntilMasteryGatesPass()
    {
        var catalog = LearningPathCatalog.CreateDefault();
        var state = LearnerState.Start("learner-1", catalog);
        var engine = new LearningProgressEngine(catalog);

        var nextAccessBeforeWork = engine.GetUnitAccess(state, "part5-verb-tense");
        Assert.False(nextAccessBeforeWork.CanStart, "Next unit must be locked before the current unit is mastered.");
        Assert.Contains(nextAccessBeforeWork.ReasonCodes, "previous_unit_incomplete");

        engine.RecordLessonViewed(state, "part5-word-form");
        engine.RecordDrillCompleted(state, "part5-word-form", correctCount: 15, totalCount: 15);
        var failedMiniTest = engine.RecordMiniTestAttempt(
            state,
            "part5-word-form",
            correctCount: 7,
            totalCount: 10,
            wrongItemIds: ["p5-word-form-007"],
            errorTag: "word_form"
        );

        Assert.False(failedMiniTest.UnitCompleted, "A 70 percent mini test must not complete the unit.");
        Assert.Equal(1, state.ReviewQueue.Count, "Wrong mini-test item should create one review item.");
        var accessAfterFailedTest = engine.GetUnitAccess(state, "part5-verb-tense");
        Assert.False(accessAfterFailedTest.CanStart, "Next unit must remain locked after failed mini test.");
        Assert.Contains(accessAfterFailedTest.ReasonCodes, "previous_unit_incomplete");

        engine.RecordReviewCompleted(state, state.ReviewQueue.Single().ReviewItemId);
        var passedMiniTest = engine.RecordMiniTestAttempt(
            state,
            "part5-word-form",
            correctCount: 9,
            totalCount: 10,
            wrongItemIds: [],
            errorTag: "word_form"
        );

        Assert.True(passedMiniTest.UnitCompleted, "Unit should complete after lesson, drill, review, and passing mini test.");
        var nextAccessAfterMastery = engine.GetUnitAccess(state, "part5-verb-tense");
        Assert.True(nextAccessAfterMastery.CanStart, "Next unit should unlock after mastery gates pass.");
    }

    public static void DemoLearnerSessionIsMarkedLegacyNonProduction()
    {
#pragma warning disable CS0618
        var obsolete = typeof(DemoLearnerSession).GetCustomAttribute<ObsoleteAttribute>();

        Assert.True(obsolete is not null, "Demo learner session must be explicitly obsolete.");
        if (obsolete is null) return;

        Assert.Equal(
            "P0.2 legacy demo-only learner flow. Do not use for production learner APIs.",
            obsolete.Message,
            "Demo learner obsolete message must block production use."
        );
        Assert.True(DemoLearnerSession.IsLegacyDemoOnly, "Demo learner session must expose demo-only marker.");
        Assert.Equal("P4", DemoLearnerSession.ReplacementPhase, "Production replacement phase must be explicit.");
#pragma warning restore CS0618
    }

    public static void BackendModuleBoundariesAreExplicit()
    {
        var domainContexts = DomainContextCatalog.All;
        var applicationContexts = ApplicationContextCatalog.All;

        Assert.Contains(domainContexts, "ContentFactory");
        Assert.Contains(domainContexts, "LearningContent");
        Assert.Contains(domainContexts, "LearnerJourney");
        Assert.Contains(domainContexts, "AttemptReview");
        Assert.Contains(domainContexts, "AnalyticsOperations");

        Assert.Equal(domainContexts.Count, applicationContexts.Count, "Application context count must match Domain.");
        foreach (var context in domainContexts)
        {
            Assert.Contains(applicationContexts, context);
        }

        var forbiddenDomainReferences = typeof(DomainContextCatalog)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is "Application" or "Infrastructure" or "Api")
            .ToList();

        Assert.Equal(0, forbiddenDomainReferences.Count, "Domain must not reference outer layers.");
    }

    public static void ProductionConfigurationRequiresExplicitDatabase()
    {
        var localOptions = ToeicPlatformOptions.FromConfiguration(
            new ConfigurationBuilder().Build(),
            "Development"
        );
        Assert.Equal(
            "Data Source=toeic-normalization.db",
            localOptions.Database.ConnectionString,
            "Development can use local SQLite default."
        );

        Assert.Throws<InvalidOperationException>(
            () => ToeicPlatformOptions.FromConfiguration(new ConfigurationBuilder().Build(), "Production"),
            "Production must require explicit ToeicDb connection string."
        );
    }
}

static class TestItems
{
    public static DraftLearningItem ValidPart5Question() => new(
        ItemType: LearningItemType.Question,
        Skill: ToeicSkill.Reading,
        Part: 5,
        Prompt: "The manager ____ the report yesterday.",
        Options: new Dictionary<string, string>
        {
            ["A"] = "submit",
            ["B"] = "submitted",
            ["C"] = "submitting",
            ["D"] = "submission",
        },
        CorrectAnswer: "B",
        Explanation: "Yesterday requires the past tense form.",
        SourceRef: new SourceRef("sheet-row-1", "drive-file-1", 12, "p12-b3"),
        Confidence: 0.92m,
        GroupRef: null,
        Word: "",
        Meaning: ""
    );
}

static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void False(bool condition, string message)
    {
        if (condition) throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }

    public static void Contains(IEnumerable<string> values, string expected)
    {
        if (!values.Contains(expected, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Expected value {expected}.");
        }
    }

    public static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
