using Application.Common.ApiContracts;
using Application.Common.Health;
using Application.Features.Dashboard.Queries;
using Application.Features.ContentCoverage;
using Application.Features.LearningItems.Commands;
using Application.Features.Learner;
using Application.Features.Learner.Home;
using Application.Features.Learner.Onboarding;
using Application.Features.Learner.Placement;
using Application.Features.Learner.Work;
using Application.Features.SourceDiscovery;
using Application.Features.SourceExtraction;
using Application.Features.SourceManifests;
using Application.Features.SourceReview;
using Application.Features.SourceValidation;
using Application.Common.Interfaces.Jobs;
using Application.Common.Interfaces.Storage;
using Application.ModuleBoundaries;
using DatabaseMigrations;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Domain.ModuleBoundaries;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Health;
using Infrastructure.Jobs;
using Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;

var tests = new List<(string Name, Action Run)>
{
    ("ToeicPlayableItem does not leak answer", ToeicItemContractsTests.ToeicPlayableItemDoesNotLeakAnswer),
    ("ToeicReviewItem contains answer keys", ToeicItemContractsTests.ToeicReviewItemContainsAnswer),
    ("Engine supports Part 1 only", ToeicPart1EngineTests.EngineSupportsPart1Only),
    ("Missing MediaAssetId throws", ToeicPart1EngineTests.MissingMediaAssetIdThrows),
    ("Invalid options throws", ToeicPart1EngineTests.InvalidOptionsThrows),
    ("Playable item hides answer", ToeicPart1EngineTests.PlayableItemHidesAnswer),
    ("Review item includes answer", ToeicPart1EngineTests.ReviewItemIncludesAnswer),
    ("valid Part 5 item publishes", ApplicationTests.ValidPart5ItemPublishes),
    ("invalid answer is rejected before persistence", ApplicationTests.InvalidAnswerIsRejectedBeforePersistence),
    ("low confidence goes to review issues", ApplicationTests.LowConfidenceGoesToReview),
    ("dashboard reports raw learning and issue counts", ApplicationTests.DashboardReportsCounts),
    ("dashboard reports corpus coverage without pretending backlog is published", ApplicationTests.DashboardReportsCorpusCoverage),
    ("content coverage baseline separates source draft validation and published layers", ApplicationTests.ContentCoverageBaselineSeparatesSourceDraftValidationAndPublishedLayers),
    ("source manifest classifier identifies provider material and access status", ApplicationTests.SourceManifestClassifierIdentifiesProviderMaterialAndAccessStatus),
    ("repository persists normalized source manifest entries", ApplicationTests.RepositoryPersistsNormalizedSourceManifestEntries),
    ("imports audited TOEIC source manifest into database", ApplicationTests.ImportsAuditedToeicSourceManifestIntoDatabase),
    ("imports local TOEIC downloads and rejects fake PDFs", ApplicationTests.ImportsLocalToeicDownloadsAndRejectsFakePdfs),
    ("discovers Drive source assets and records blocked issues", ApplicationTests.DiscoversDriveSourceAssetsAndRecordsBlockedIssues),
    ("resolves TOEIC external sources and shortlinks", ApplicationTests.ResolvesToeicExternalSourcesAndShortlinks),
    ("registers TOEIC source assets from audit evidence", ApplicationTests.RegistersToeicSourceAssetsFromAuditEvidence),
    ("extracts TOEIC PDF pages and text blocks", ApplicationTests.ExtractsToeicPdfPagesAndTextBlocks),
    ("extracts TOEIC audio metadata", ApplicationTests.ExtractsToeicAudioMetadata),
    ("parses TOEIC answer keys into draft mappings", ApplicationTests.ParsesToeicAnswerKeysIntoDraftMappings),
    ("parses TOEIC transcripts into draft segments", ApplicationTests.ParsesToeicTranscriptsIntoDraftSegments),
    ("parses TOEIC reading drafts with tags and source trace", ApplicationTests.ParsesToeicReadingDraftsWithTagsAndSourceTrace),
    ("parses TOEIC listening draft groups", ApplicationTests.ParsesToeicListeningDraftGroups),
    ("validates TOEIC draft content and records issues", ApplicationTests.ValidatesToeicDraftContentAndRecordsIssues),
    ("reviews and publishes approved TOEIC draft content", ApplicationTests.ReviewsAndPublishesApprovedToeicDraftContent),
    ("onboards learner and returns next placement action", ApplicationTests.OnboardsLearnerAndReturnsNextPlacementAction),
    ("learner home reads persisted profile after restart", ApplicationTests.LearnerHomeReadsPersistedProfileAfterRestart),
    ("starts placement session and resumes active duplicate", ApplicationTests.StartsPlacementSessionAndResumesActiveDuplicate),
    ("scores placement session and persists result", ApplicationTests.ScoresPlacementSessionAndPersistsResult),
    ("generates learner path from placement result", ApplicationTests.GeneratesLearnerPathFromPlacementResult),
    ("assigns learner today plan", ApplicationTests.AssignsLearnerTodayPlan),
    ("manages learner activity sessions", ApplicationTests.ManagesActivitySessions),
    ("processes learner attempts", ApplicationTests.ProcessesLearnerAttempts),
    ("review queue creates and updates items", ApplicationTests.ReviewQueueCreatesAndUpdatesReviewItems),
    ("review queue resolves review items", ApplicationTests.ReviewQueueResolvesReviewItems),
    ("review queue orders correctly", ApplicationTests.ReviewQueueOrdersCorrectly),
    ("dashboard includes normalized source manifest summary", ApplicationTests.DashboardIncludesNormalizedSourceManifestSummary),
    ("learner cannot unlock next unit until mastery gates pass", ApplicationTests.LearnerCannotUnlockNextUnitUntilMasteryGatesPass),
    ("mastery calculation service updates blockers and mastery record", ApplicationTests.MasteryCalculationServiceUpdatesBlockers),
    ("failed mini test keeps next unit locked", ApplicationTests.FailedMiniTestKeepsNextUnitLocked),
    ("passed mini test unlocks next unit", ApplicationTests.PassedMiniTestUnlocksNextUnit),
    ("demo learner session is marked legacy non-production", ApplicationTests.DemoLearnerSessionIsMarkedLegacyNonProduction),
    ("backend module boundaries are explicit", ApplicationTests.BackendModuleBoundariesAreExplicit),
    ("production configuration requires explicit database", ApplicationTests.ProductionConfigurationRequiresExplicitDatabase),
    ("postgres migration foundation is explicit", ApplicationTests.PostgresMigrationFoundationIsExplicit),
    ("object storage test double stores lists and deletes objects", ApplicationTests.ObjectStorageTestDoubleStoresListsAndDeletesObjects),
    ("background job queue retries then records failure", ApplicationTests.BackgroundJobQueueRetriesThenRecordsFailure),
    ("api contract catalog defines stable typed routes", ApplicationTests.ApiContractCatalogDefinesStableTypedRoutes),
    ("platform health reports dependency readiness", ApplicationTests.PlatformHealthReportsDependencyReadiness),
    ("repository persists source containers and assets idempotently", ApplicationTests.RepositoryPersistsSourceContainersAndAssetsIdempotently),
    ("repository persists extracted pages and text blocks idempotently", ApplicationTests.RepositoryPersistsExtractedPagesAndTextBlocksIdempotently),
    ("repository persists draft content safely away from learner contracts", ApplicationTests.RepositoryPersistsDraftContentSafelyAwayFromLearnerContracts),
    ("repository persists published lessons and guided examples", ApplicationTests.RepositoryPersistsPublishedLessonsAndGuidedExamples),
    ("repository persists published questions and enforces part rules", ApplicationTests.RepositoryPersistsPublishedQuestionsAndEnforcesPartRules),
    ("repository persists TOEIC tests sections and ordered items", ApplicationTests.RepositoryPersistsToeicTestsSectionsAndOrderedItems),
    ("repository persists learner profiles across restart", ApplicationTests.RepositoryPersistsLearnerProfilesAcrossRestart),
    ("repository persists learner assignments sessions attempts and answers", ApplicationTests.RepositoryPersistsLearnerAssignmentsSessionsAttemptsAndAnswers),
    ("repository persists review and mastery records", ApplicationTests.RepositoryPersistsReviewAndMasteryRecords),
    ("repository enforces TOEIC data integrity and indexes", ApplicationTests.RepositoryEnforcesToeicDataIntegrityAndIndexes),
    ("performance baseline get learner today plan", ApplicationTests.PerformanceBaselineGetLearnerTodayPlan),
    ("repository persists source asset links", ApplicationTests.RepositoryPersistsSourceAssetLinks),
    ("ToeicAnswerKeyParser extracts keys", ApplicationTests.ToeicAnswerKeyParserExtractsKeys),
    ("ToeicAnswerKeyParser uses profiles", ApplicationTests.ToeicAnswerKeyParserUsesProfiles),
    ("ToeicTranscriptParser extracts dialogs", ApplicationTests.ToeicTranscriptParserExtractsDialogs),
    ("LinkSourceAssetsHandler pairs assets", ApplicationTests.LinkSourceAssetsHandlerPairsAssets),
    ("Today plan read does not mutate DB", ApplicationTests.TodayPlanReadDoesNotMutateDb),
    ("mastery recalculates on attempt submit and review", ApplicationTests.MasteryRecalculatesOnAttemptSubmitAndReview),
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
    public static void MasteryCalculationServiceUpdatesBlockers()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var learnerId = "learner-mastery";
        var pathId = "path-1";
        
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile(learnerId, "Mastery Learner", "mastery@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        repository.UpsertLearningPath(new Domain.Aggregates.LearnerProgress.LearningPath(pathId, learnerId, Domain.Aggregates.LearnerProgress.LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var unit1 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u1", pathId, "key1", 5, "tag", 1, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Unlocked, null, null);
        var unit2 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u2", pathId, "key2", 5, "tag", 2, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Locked, null, null);
        repository.UpsertLearningPathUnit(unit1);
        repository.UpsertLearningPathUnit(unit2);
        
        var service = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
        service.RecalculateMastery(learnerId, "u1");
        
        var blockers = repository.GetUnlockBlockers(learnerId, "u1");
        Assert.True(blockers.Any(b => b.Reason == "LESSON_NOT_COMPLETED"), "Missing lesson should block unit completion");
        
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a1", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Lesson, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s2", "a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att2", "s2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 10, 10, 100, DateTimeOffset.UtcNow));

        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s3", "a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att3", "s3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 10, 10, 100, DateTimeOffset.UtcNow));
        
        service.RecalculateMastery(learnerId, "u1");
        blockers = repository.GetUnlockBlockers(learnerId, "u1");
        Assert.True(blockers.Count == 0, "All assignments completed should remove blockers");

        var record = repository.GetMasteryRecord(learnerId, "u1");
        Assert.True(record is not null, "Mastery record should be created");
        Assert.True(record!.IsUnlocked, "Unit should be unlocked");
        Assert.True(record.MasteryPercent == 100, "Mastery should be 100%");
    }

    public static void FailedMiniTestKeepsNextUnitLocked()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var learnerId = "learner-fail-test";
        var pathId = "path-1";
        
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile(learnerId, "Fail Learner", "fail@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new Domain.Aggregates.LearnerProgress.LearningPath(pathId, learnerId, Domain.Aggregates.LearnerProgress.LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var unit1 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u1", pathId, "key1", 5, "tag", 1, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Unlocked, null, null);
        var unit2 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u2", pathId, "key2", 5, "tag", 2, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Locked, null, null);
        repository.UpsertLearningPathUnit(unit1);
        repository.UpsertLearningPathUnit(unit2);

        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a1", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Lesson, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s2", "a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att2", "s2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 10, 10, 100, DateTimeOffset.UtcNow));

        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s3", "a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        // Failed with 70%
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att3", "s3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 7, 10, 70, DateTimeOffset.UtcNow));

        var service = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
        service.RecalculateMastery(learnerId, "u1");

        var blockers = repository.GetUnlockBlockers(learnerId, "u1");
        Assert.True(blockers.Any(b => b.Reason == "MINI_TEST_NOT_PASSED"), "Mini test not passed blocker should exist");

        var updatedUnit2 = repository.GetLearningPathUnits(pathId).FirstOrDefault(u => u.UnitId == "u2");
        Assert.True(updatedUnit2?.Status == Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Locked, "Unit 2 should remain locked");
    }

    public static void PassedMiniTestUnlocksNextUnit()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var learnerId = "learner-pass-test";
        var pathId = "path-1";
        
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile(learnerId, "Pass Learner", "pass@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new Domain.Aggregates.LearnerProgress.LearningPath(pathId, learnerId, Domain.Aggregates.LearnerProgress.LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var unit1 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u1", pathId, "key1", 5, "tag", 1, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Unlocked, null, null);
        var unit2 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u2", pathId, "key2", 5, "tag", 2, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Locked, null, null);
        repository.UpsertLearningPathUnit(unit1);
        repository.UpsertLearningPathUnit(unit2);

        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a1", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Lesson, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s2", "a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att2", "s2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 10, 10, 100, DateTimeOffset.UtcNow));

        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s3", "a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        // Passed with 85%
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att3", "s3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 8, 10, 85, DateTimeOffset.UtcNow));

        var service = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
        service.RecalculateMastery(learnerId, "u1");

        var blockers = repository.GetUnlockBlockers(learnerId, "u1");
        Assert.True(blockers.Count == 0, "All assignments completed and passed should remove blockers");

        var updatedUnit1 = repository.GetLearningPathUnits(pathId).FirstOrDefault(u => u.UnitId == "u1");
        var updatedUnit2 = repository.GetLearningPathUnits(pathId).FirstOrDefault(u => u.UnitId == "u2");
        Assert.True(updatedUnit1?.Status == Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Completed, "Unit 1 should be completed");
        Assert.True(updatedUnit2?.Status == Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Unlocked, "Unit 2 should be unlocked");
    }

    public static void MasteryRecalculatesOnAttemptSubmitAndReview()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var learnerId = "learner-submit-mastery";
        var pathId = "path-1";
        
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile(learnerId, "Mastery Submitter", "test@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new Domain.Aggregates.LearnerProgress.LearningPath(pathId, learnerId, Domain.Aggregates.LearnerProgress.LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var unit1 = new Domain.Aggregates.LearnerProgress.LearningPathUnit("u1", pathId, "key1", 5, "tag", 1, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Unlocked, null, null);
        repository.UpsertLearningPathUnit(unit1);

        // Pre-requisites for passing mini test: Lesson, Drill completed
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a1", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Lesson, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Completed, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s2", "a2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.Drill, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerAttempt(new Domain.Aggregates.LearnerProgress.LearnerAttempt("att2", "s2", learnerId, Domain.Aggregates.LearnerProgress.LearnerAttemptStatus.Scored, 10, 10, 100, DateTimeOffset.UtcNow));

        // Now setup the MiniTest to be submitted
        repository.UpsertLearnerAssignment(new Domain.Aggregates.LearnerProgress.LearnerAssignment("a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, "u1", Domain.Aggregates.LearnerProgress.LearnerAssignmentStatus.Started, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new Domain.Aggregates.LearnerProgress.ActivitySession("s3", "a3", learnerId, Domain.Aggregates.LearnerProgress.LearnerAssignmentType.MiniTest, Domain.Aggregates.LearnerProgress.ActivitySessionStatus.InProgress, DateTimeOffset.UtcNow, null));
        
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 5, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        repository.UpsertPublishedQuestion(new PublishedQuestion("q1", "l1", 5, PublishedQuestionType.SingleQuestion, "prompt", "{}", "A", "exp", null, null, null, "{}", "tags", "{}", PublishedContentStatus.Published));

        var handler = new Application.Features.Learner.Work.SubmitAttemptHandler(repository);
        handler.Handle(new Application.Features.Learner.Work.SubmitAttemptCommand("s3", learnerId, new Dictionary<string, string> { { "q1", "A" } }));

        var record = repository.GetMasteryRecord(learnerId, "u1");
        Assert.True(record is not null, "Mastery record should be updated upon submit attempt");
        Assert.True(record!.IsUnlocked, "Mastery should be updated to unlocked");
    }

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

    public static void ContentCoverageBaselineSeparatesSourceDraftValidationAndPublishedLayers()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        new ImportToeicSourceManifestHandler(repository).Handle();
        var asset = SeedSourceAsset(repository);
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-ready-part5",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"prompt":"The team needs a more ____ plan.","options":{"A":"effect","B":"effective","C":"effectively","D":"effectiveness"},"correctAnswer":"B","skillTags":["word_form"]}""",
            SourceTraceJson: """{"sourceId":"sheet-row-7","assetId":"asset-sparta-test-01-pdf"}""",
            ParserConfidence: 0.94m,
            Status: DraftContentStatus.ReadyForReview
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-invalid-part7",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 7,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"prompt":"Missing passage","options":{"A":"x"},"correctAnswer":"A","skillTags":["inference"]}""",
            SourceTraceJson: """{"sourceId":"sheet-row-7","assetId":"asset-sparta-test-01-pdf"}""",
            ParserConfidence: 0.62m,
            Status: DraftContentStatus.ValidationFailed
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-pending-part3",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 3,
            ItemType: "ListeningQuestion",
            PayloadJson: """{"prompt":"What does the woman imply?","options":{"A":"x"},"correctAnswer":"A","skillTags":["inference"]}""",
            SourceTraceJson: """{"sourceId":"sheet-row-12","assetId":"asset-sparta-test-01-pdf"}""",
            ParserConfidence: 0.89m,
            Status: DraftContentStatus.PendingValidation
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-rejected-part3",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 3,
            ItemType: "ListeningQuestion",
            PayloadJson: """{"prompt":"Rejected OCR row","options":{"A":"x"},"correctAnswer":"A","skillTags":["ocr"]}""",
            SourceTraceJson: """{"sourceId":"sheet-row-12","assetId":"asset-sparta-test-01-pdf"}""",
            ParserConfidence: 0.86m,
            Status: DraftContentStatus.Rejected
        ));
        repository.RecordValidationIssue(
            new ValidationIssue("missing_passage", "Part 7 draft must include passage evidence."),
            "ReadingQuestion",
            "sheet-row-7"
        );
        repository.RecordValidationIssue(
            new ValidationIssue("missing_passage", "Second Part 7 draft must include passage evidence."),
            "ReadingQuestion",
            "sheet-row-9"
        );
        repository.RecordValidationIssue(
            new ValidationIssue("invalid_answer_options", "Correct answer must exist in options."),
            "ReadingQuestion",
            "sheet-row-11"
        );
        repository.UpsertPublishedLesson(new PublishedLesson(
            LessonId: "lesson-part5-word-form",
            UnitId: "part5-word-form",
            ToeicPart: 5,
            Title: "Word Form Foundations",
            Objective: "Choose the correct part of speech from sentence position.",
            SkillTags: "word_form,grammar",
            SourceTraceJson: """{"sourceId":"sheet-row-7","draftId":"draft-ready-part5"}""",
            Status: PublishedContentStatus.Published
        ));
        repository.UpsertPublishedQuestion(new PublishedQuestion(
            QuestionId: "question-part5-word-form-001",
            LessonId: "lesson-part5-word-form",
            ToeicPart: 5,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "The team needs a more ____ plan.",
            OptionsJson: """{"A":"effect","B":"effective","C":"effectively","D":"effectiveness"}""",
            CorrectAnswer: "B",
            Explanation: "The blank before a noun needs an adjective.",
            MediaAssetId: null,
            PassageId: null,
            GroupId: null,
            EvidenceJson: """{"sourceId":"sheet-row-7","draftId":"draft-ready-part5"}""",
            SkillTags: "word_form,grammar",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf"}""",
            Status: PublishedContentStatus.Published
        ));

        var coverage = new GetContentCoverageHandler(repository).Handle();

        Assert.Equal(73, coverage.Source.TotalSources, "Coverage must include audited source manifest count.");
        Assert.Equal(60, coverage.Source.AccessibleSources, "Coverage must preserve accessible source count.");
        Assert.Equal(4, coverage.Draft.TotalDraftItems, "Draft count must stay separate from published content.");
        Assert.Equal(1, coverage.Draft.PendingValidationDraftItems, "Pending draft count should be explicit.");
        Assert.Equal(1, coverage.Draft.ReadyForReviewDraftItems, "Ready draft count should be explicit.");
        Assert.Equal(1, coverage.Draft.ValidationFailedDraftItems, "Validation failed draft count should be explicit.");
        Assert.Equal(1, coverage.Draft.RejectedDraftItems, "Rejected draft count should be explicit.");
        Assert.Equal(3, coverage.Validation.ValidationIssueCount, "Validation issue count should be separate.");
        Assert.Equal(2, coverage.Validation.IssueBreakdown.Single(issue => issue.Code == "missing_passage").Count, "Coverage should group repeated validation issues.");
        Assert.Equal(1, coverage.Validation.IssueBreakdown.Single(issue => issue.Code == "invalid_answer_options").Count, "Coverage should expose each validation issue code.");
        Assert.Equal(1, coverage.Published.PublishedQuestions, "Only approved published rows count as learner content.");
        Assert.Equal(0, coverage.Published.PublishedTests, "Missing test data must not be faked.");
        var part3 = coverage.ToeicParts.Single(part => part.ToeicPart == 3);
        var part5 = coverage.ToeicParts.Single(part => part.ToeicPart == 5);
        var part7 = coverage.ToeicParts.Single(part => part.ToeicPart == 7);
        Assert.Equal(2, part3.TotalDraftItems, "Part 3 should expose total draft backlog.");
        Assert.Equal(1, part3.PendingValidationDraftItems, "Part 3 should expose pending draft backlog.");
        Assert.Equal(1, part3.RejectedDraftItems, "Part 3 should expose rejected draft backlog.");
        Assert.Equal(1, part5.TotalDraftItems, "Part 5 should expose total draft backlog.");
        Assert.Equal(1, part5.ReadyForReviewDraftItems, "Part 5 should expose ready draft backlog.");
        Assert.Equal(0, part5.ValidationFailedDraftItems, "Part 5 should not report Part 7 failures.");
        Assert.Equal(1, part5.PublishedQuestions, "Part 5 should report real published question count.");
        Assert.Equal(1, part5.PublishedLessons, "Part 5 should report real published lesson count.");
        Assert.Equal(1, part7.TotalDraftItems, "Part 7 should expose total draft backlog.");
        Assert.Equal(0, part7.ReadyForReviewDraftItems, "Part 7 should not inherit Part 5 ready drafts.");
        Assert.Equal(1, part7.ValidationFailedDraftItems, "Part 7 should expose failed draft backlog.");
        Assert.Equal(0, part7.PublishedQuestions, "Part 7 source backlog must not appear as published.");
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
        Assert.Equal(60, result.AccessibleCount, "Expected accessible source count from audit.");
        Assert.Equal(13, result.BlockedCount, "Expected blocked source count from audit.");
        Assert.Equal(33, result.SourcesWithPdf, "Expected PDF evidence count from audit.");
        Assert.Equal(20, result.SourcesWithAudio, "Expected audio evidence count from audit.");
        Assert.Equal(11, result.SourcesWithImage, "Expected image evidence count from audit.");
        Assert.Equal(6, result.SourcesWithTranscript, "Expected transcript evidence count from audit.");
        Assert.Equal(5, result.SourcesWithAnswerKey, "Expected answer-key evidence count from audit.");
        Assert.Equal(73, repository.Count("source_manifest_entries"), "Expected DB rows.");
        handler.Handle();
        Assert.Equal(73, repository.Count("source_manifest_entries"), "Repeated import must update rows instead of duplicating them.");
        var summary = new GetSourceManifestSummaryHandler(repository).Handle();
        Assert.Equal(36, summary.DriveFolders, "Expected Drive folder count from audit.");
        Assert.Equal(14, summary.DriveFiles, "Expected Drive file count from audit.");
        Assert.Equal(4, summary.Shortlinks, "Expected shortlink count from audit.");
    }

    public static void ImportsLocalToeicDownloadsAndRejectsFakePdfs()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var root = Path.Combine(Path.GetTempPath(), $"toeic-local-downloads-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "folders", "Sparta Toeic"));
        var validPdfPath = Path.Combine(root, "folders", "Sparta Toeic", "Đáp án (answer key) Sách Sparta TOEIC.pdf");
        var fakePdfPath = Path.Combine(root, "folders", "Sparta Toeic", "video-1.pdf");
        var notePath = Path.Combine(root, "folders", "Sparta Toeic", "note.txt");
        File.WriteAllBytes(validPdfPath, Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj\n"));
        File.WriteAllText(fakePdfPath, "<html>Google Drive sign in</html>");
        File.WriteAllText(notePath, "not a pdf");

        try
        {
            var handler = new ImportLocalToeicDownloadsHandler(repository);
            var result = handler.Handle(new ImportLocalToeicDownloadsCommand(root));
            var secondResult = handler.Handle(new ImportLocalToeicDownloadsCommand(root));

            Assert.Equal(3, result.ScannedFileCount, "Importer should scan files recursively.");
            Assert.Equal(1, result.ImportedPdfCount, "Only valid PDF bytes should be imported.");
            Assert.Equal(2, result.RejectedFileCount, "Fake PDF and non-PDF files must be rejected.");
            Assert.Equal(1, secondResult.ImportedPdfCount, "Repeated import should still report the valid source.");
            Assert.Equal(1, repository.Count("source_manifest_entries"), "Repeated import must not duplicate local source rows.");
            Assert.Equal(1, repository.Count("source_containers"), "Repeated import must not duplicate containers.");
            Assert.Equal(1, repository.Count("source_assets"), "Repeated import must not duplicate assets.");
            var source = repository.GetSourceManifestEntries().Single();
            Assert.Equal(MaterialClass.TestBook, source.PrimaryMaterialClass, "Local TOEIC PDF should be classified by title.");
            Assert.True(source.Evidence.HasPdf, "Local PDF source must record PDF evidence.");
            Assert.True(source.Evidence.HasAnswerKey, "Answer key PDFs should be flagged for later parsing.");
            Assert.True(source.AuditNotes.Contains("downloads local corpus", StringComparison.Ordinal), "Audit note should identify local corpus origin.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    public static void DiscoversDriveSourceAssetsAndRecordsBlockedIssues()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var accessible = SourceManifestClassifier.Classify(
            7,
            "SPARTA TOEIC ( quyển hồng - 10TEST )",
            "https://drive.google.com/drive/folders/audit-source-7",
            inaccessible: false,
            hasPdf: true,
            hasAudio: true,
            hasTranscript: false,
            hasAnswerKey: true,
            hasImage: true
        );
        var blocked = SourceManifestClassifier.Classify(
            36,
            "Khóa tiếng anh giao tiếp file ZIP",
            "https://drive.google.com/drive/folders/audit-source-36",
            inaccessible: true,
            hasPdf: false,
            hasAudio: false,
            hasTranscript: false,
            hasAnswerKey: false,
            hasImage: false
        );
        repository.UpsertSourceManifestEntry(accessible);
        repository.UpsertSourceManifestEntry(blocked);
        var gateway = new FakeDriveDiscoveryGateway([
            new DriveDiscoveredAsset(
                ExternalId: "drive-pdf-001",
                FileName: "sparta-test-01.pdf",
                MimeType: "application/pdf",
                Extension: ".pdf",
                SizeBytes: 1_200_000,
                ProviderUrl: "https://drive.google.com/file/d/drive-pdf-001/view",
                Checksum: "sha256-pdf-001"
            ),
            new DriveDiscoveredAsset(
                ExternalId: "drive-audio-001",
                FileName: "sparta-test-01.mp3",
                MimeType: "audio/mpeg",
                Extension: ".mp3",
                SizeBytes: 540_000,
                ProviderUrl: "https://drive.google.com/file/d/drive-audio-001/view",
                Checksum: "sha256-audio-001"
            ),
        ]);
        var handler = new DiscoverDriveSourceAssetsHandler(repository, gateway);

        var result = handler.Handle(new DiscoverDriveSourceAssetsCommand());

        Assert.Equal(1, result.DiscoveredContainerCount, "Only accessible Drive folder should become a discovered container.");
        Assert.Equal(2, result.DiscoveredAssetCount, "Drive folder children should become source assets.");
        Assert.Equal(1, result.BlockedIssueCount, "Blocked Drive source should create one discovery issue.");
        Assert.Equal(1, repository.Count("source_containers"), "Expected one source container.");
        Assert.Equal(2, repository.Count("source_assets"), "Expected two source assets.");
        Assert.Equal(1, repository.Count("source_discovery_issues"), "Expected one blocked source issue.");
        var pdfAsset = repository.GetSourceAssets("drive-folder-audit-source-7").Single(asset => asset.FileName.EndsWith(".pdf", StringComparison.Ordinal));
        Assert.Equal(SourceAssetRole.Pdf, pdfAsset.DetectedRole, "PDF role should be detected.");
        Assert.Equal(SourceDiscoveryIssueStatus.Open, repository.GetSourceDiscoveryIssues(blocked.SourceId).Single().Status, "Blocked issue should remain open.");
    }

    public static void ResolvesToeicExternalSourcesAndShortlinks()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var shortlink = SourceManifestClassifier.Classify(
            67,
            "Phương pháp nâng cấp Speaking",
            "https://tinyurl.com/toeic-audit-67",
            inaccessible: false,
            hasPdf: false,
            hasAudio: false,
            hasTranscript: false,
            hasAnswerKey: false,
            hasImage: false
        );
        var external = SourceManifestClassifier.Classify(
            38,
            "Dễ dàng đạt Listening 750+ - Unica",
            "https://toeic-source.example/materials/38",
            inaccessible: false,
            hasPdf: false,
            hasAudio: true,
            hasTranscript: false,
            hasAnswerKey: false,
            hasImage: false
        );
        repository.UpsertSourceManifestEntry(shortlink);
        repository.UpsertSourceManifestEntry(external);
        var resolver = new FakeExternalSourceResolver(new Dictionary<string, ExternalSourceResolutionResult>
        {
            [shortlink.Url] = new(
                OriginalUrl: shortlink.Url,
                ResolvedUrl: "https://youtube.com/watch?v=toeic-speaking-method",
                HttpStatusCode: 200,
                RedirectCount: 2
            ),
            [external.Url] = new(
                OriginalUrl: external.Url,
                ResolvedUrl: "https://unica.vn/toeic-listening-750",
                HttpStatusCode: 200,
                RedirectCount: 1
            ),
        });
        var handler = new ResolveToeicExternalSourcesHandler(repository, resolver);

        var result = handler.Handle(new ResolveToeicExternalSourcesCommand());

        Assert.Equal(2, result.ResolvedCount, "Shortlink and external web sources should resolve.");
        Assert.Equal(0, result.FailedCount, "Valid resolver responses should not fail.");
        Assert.Equal(2, repository.Count("source_resolution_records"), "Resolution records should persist.");
        var resolutions = repository.GetSourceResolutionRecords();
        Assert.True(resolutions.Any(record => record.ResolvedUrl.Contains("youtube.com", StringComparison.Ordinal)), "Shortlink final URL should persist.");
        Assert.True(resolutions.All(record => record.Status == SourceResolutionStatus.Resolved), "All resolver successes should be marked resolved.");
    }

    public static void RegistersToeicSourceAssetsFromAuditEvidence()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var source = SourceManifestClassifier.Classify(
            46,
            "1000 câu giải đề Format mới",
            "https://drive.google.com/file/d/audit-source-46/view",
            inaccessible: false,
            hasPdf: true,
            hasAudio: false,
            hasTranscript: true,
            hasAnswerKey: true,
            hasImage: true
        );
        var blocked = SourceManifestClassifier.Classify(
            62,
            "Advanced Grammar in Use",
            "https://drive.google.com/file/d/audit-source-62/view",
            inaccessible: true,
            hasPdf: true,
            hasAudio: true,
            hasTranscript: false,
            hasAnswerKey: false,
            hasImage: true
        );
        repository.UpsertSourceManifestEntry(source);
        repository.UpsertSourceManifestEntry(blocked);
        var handler = new RegisterToeicSourceAssetsHandler(repository);

        var result = handler.Handle(new RegisterToeicSourceAssetsCommand());

        Assert.Equal(1, result.RegisteredContainerCount, "Accessible evidence source should create one registration container.");
        Assert.Equal(4, result.RegisteredAssetCount, "PDF, image, transcript, and answer key evidence should become registered assets.");
        Assert.Equal(1, result.SkippedBlockedSourceCount, "Blocked source should be skipped.");
        Assert.Equal(1, repository.Count("source_containers"), "Expected one source container.");
        Assert.Equal(4, repository.Count("source_assets"), "Expected all registered evidence assets.");
        var assets = repository.GetSourceAssets("registered-source-sheet-row-46");
        Assert.True(assets.Any(asset => asset.DetectedRole == SourceAssetRole.Pdf), "PDF role should be registered.");
        Assert.True(assets.Any(asset => asset.DetectedRole == SourceAssetRole.Image), "Image role should be registered.");
        Assert.True(assets.Any(asset => asset.DetectedRole == SourceAssetRole.Transcript), "Transcript role should be registered.");
        Assert.True(assets.Any(asset => asset.DetectedRole == SourceAssetRole.AnswerKey), "Answer key role should be registered.");
        Assert.False(assets.Any(asset => asset.DetectedRole == SourceAssetRole.Audio), "Missing audio evidence must not create audio asset.");
    }

    public static void ExtractsToeicPdfPagesAndTextBlocks()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var asset = SeedSourceAsset(repository);
        var extractor = new FakePdfTextBlockExtractor([
            new PdfExtractedPageResult(
                PageNumber: 1,
                Width: 595,
                Height: 842,
                Blocks:
                [
                    new PdfExtractedTextBlockResult(ExtractedBlockType.Heading, "TEST 01", 0.98m, """{"x":10,"y":10,"w":100,"h":20}"""),
                    new PdfExtractedTextBlockResult(ExtractedBlockType.Question, "The manager ____ the report yesterday.", 0.94m, """{"x":10,"y":60,"w":300,"h":30}"""),
                ]
            ),
        ]);
        var handler = new ExtractToeicPdfBlocksHandler(repository, extractor);

        var result = handler.Handle(new ExtractToeicPdfBlocksCommand(asset.AssetId));

        Assert.Equal(1, result.ExtractedPageCount, "Fixture PDF should create one extracted page.");
        Assert.Equal(2, result.ExtractedBlockCount, "Fixture PDF should create two extracted text blocks.");
        Assert.Equal(1, repository.Count("extracted_pages"), "Extracted page should persist.");
        Assert.Equal(2, repository.Count("extracted_text_blocks"), "Extracted blocks should persist.");
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        Assert.Equal(ExtractedBlockType.Heading, blocks[0].BlockType, "Heading block type should persist.");
        Assert.Equal(0.94m, blocks[1].Confidence, "Block confidence should persist.");
        Assert.True(blocks[1].Text.Contains("manager", StringComparison.Ordinal), "Extracted text should persist.");
    }

    public static void ExtractsToeicAudioMetadata()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var audioAsset = SeedSourceAsset(repository) with
        {
            AssetId = "asset-sparta-test-01-audio",
            FileName = "test-01.mp3",
            MimeType = "audio/mpeg",
            Extension = ".mp3",
            DetectedRole = SourceAssetRole.Audio,
            ProviderUrl = "https://drive.google.com/file/d/audio",
            ObjectKey = "source-assets/sparta/test-01.mp3",
            Checksum = "sha256-audio",
        };
        repository.UpsertSourceAsset(audioAsset);
        var probe = new FakeAudioMetadataProbe(new AudioMetadataProbeResult(
            DurationSeconds: 125,
            Format: "mp3",
            SampleRateHz: 44_100,
            BitrateKbps: 192
        ));
        var handler = new ExtractToeicAudioMetadataHandler(repository, probe);

        var result = handler.Handle(new ExtractToeicAudioMetadataCommand(audioAsset.AssetId));

        Assert.Equal(1, result.ExtractedAudioMetadataCount, "Audio metadata should be extracted.");
        Assert.Equal(1, repository.Count("source_audio_metadata"), "Audio metadata row should persist.");
        var metadata = repository.GetSourceAudioMetadata(audioAsset.AssetId);
        Assert.True(metadata is not null, "Audio metadata should be queryable by asset.");
        if (metadata is null) return;

        Assert.Equal(125, metadata.DurationSeconds, "Audio duration should persist.");
        Assert.Equal("mp3", metadata.Format, "Audio format should persist.");
        Assert.Equal(44_100, metadata.SampleRateHz, "Sample rate should persist.");
        Assert.Equal(192, metadata.BitrateKbps, "Bitrate should persist.");
    }

    public static void ParsesToeicAnswerKeysIntoDraftMappings()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var answerKeyAsset = SeedSourceAsset(repository) with
        {
            AssetId = "asset-sparta-answer-key",
            FileName = "answer-key.pdf",
            DetectedRole = SourceAssetRole.AnswerKey,
            ProviderUrl = "https://drive.google.com/file/d/answer-key",
            ObjectKey = "source-assets/sparta/answer-key.pdf",
            Checksum = "sha256-answer-key",
        };
        repository.UpsertSourceAsset(answerKeyAsset);
        var parser = new FakeAnswerKeyParser([
            new AnswerKeyMappingResult(TestId: "sparta-test-01", QuestionNumber: 1, CorrectAnswer: "A", Confidence: 0.96m),
            new AnswerKeyMappingResult(TestId: "sparta-test-01", QuestionNumber: 2, CorrectAnswer: "C", Confidence: 0.94m),
        ]);
        var handler = new ParseToeicAnswerKeysHandler(repository, parser);

        var result = handler.Handle(new ParseToeicAnswerKeysCommand(answerKeyAsset.AssetId));

        Assert.Equal(2, result.CreatedDraftMappingCount, "Two answer mappings should become draft records.");
        Assert.Equal(2, repository.Count("draft_content_items"), "Answer key draft rows should persist.");
        var drafts = repository.GetDraftContentItems(answerKeyAsset.AssetId);
        Assert.True(drafts.All(draft => draft.ItemType == "AnswerKeyMapping"), "Draft item type should identify answer key mappings.");
        Assert.True(drafts.Any(draft => draft.PayloadJson.Contains("\"correctAnswer\":\"A\"", StringComparison.Ordinal)), "Correct answer should persist in payload.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"schemaVersion\":\"toeic-draft.v1\"", StringComparison.Ordinal)), "Draft payload should be versioned.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"kind\":\"AnswerKeyMapping\"", StringComparison.Ordinal)), "Draft payload should declare its canonical kind.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"data\":", StringComparison.Ordinal)), "Draft payload should wrap parser data.");
        Assert.True(drafts.All(draft => draft.ParserConfidence >= 0.94m), "Parser confidence should persist.");
    }

    public static void ParsesToeicTranscriptsIntoDraftSegments()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var transcriptAsset = SeedSourceAsset(repository) with
        {
            AssetId = "asset-sparta-transcript",
            FileName = "transcript-test-01.txt",
            MimeType = "text/plain",
            Extension = ".txt",
            DetectedRole = SourceAssetRole.Transcript,
            ProviderUrl = "https://drive.google.com/file/d/transcript",
            ObjectKey = "source-assets/sparta/transcript-test-01.txt",
            Checksum = "sha256-transcript",
        };
        repository.UpsertSourceAsset(transcriptAsset);
        var parser = new FakeTranscriptParser([
            new TranscriptSegmentResult(
                TestGroupId: "sparta-test-01-part3-group-01",
                LinkedAudioAssetId: "asset-sparta-test-01-audio",
                SpeakerLabel: "Woman",
                Text: "Could you send me the revised contract?",
                StartSecond: 12,
                EndSecond: 16,
                Confidence: 0.93m
            ),
        ]);
        var handler = new ParseToeicTranscriptsHandler(repository, parser);

        var result = handler.Handle(new ParseToeicTranscriptsCommand(transcriptAsset.AssetId));

        Assert.Equal(1, result.CreatedTranscriptSegmentCount, "One transcript segment should become draft content.");
        Assert.Equal(1, repository.Count("draft_content_items"), "Transcript draft row should persist.");
        var draft = repository.GetDraftContentItems(transcriptAsset.AssetId).Single();
        Assert.Equal("TranscriptSegment", draft.ItemType, "Draft item type should identify transcript segment.");
        Assert.True(draft.PayloadJson.Contains("\"schemaVersion\":\"toeic-draft.v1\"", StringComparison.Ordinal), "Draft payload should be versioned.");
        Assert.True(draft.PayloadJson.Contains("\"kind\":\"TranscriptSegment\"", StringComparison.Ordinal), "Draft payload should declare its canonical kind.");
        Assert.True(draft.PayloadJson.Contains("\"data\":", StringComparison.Ordinal), "Draft payload should wrap parser data.");
        Assert.True(draft.PayloadJson.Contains("sparta-test-01-part3-group-01", StringComparison.Ordinal), "Transcript should link to test group.");
        Assert.True(draft.PayloadJson.Contains("asset-sparta-test-01-audio", StringComparison.Ordinal), "Transcript should link to audio asset.");
        Assert.Equal(0.93m, draft.ParserConfidence, "Transcript parser confidence should persist.");
    }

    public static void ParsesToeicReadingDraftsWithTagsAndSourceTrace()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var asset = SeedSourceAsset(repository);
        repository.UpsertExtractedPage(new ExtractedPage(
            PageId: "page-reading-001",
            AssetId: asset.AssetId,
            PageNumber: 1,
            Width: 595,
            Height: 842,
            ExtractedAtUtc: new DateTimeOffset(2026, 7, 2, 5, 0, 0, TimeSpan.Zero)
        ));
        repository.UpsertExtractedTextBlock(new ExtractedTextBlock(
            BlockId: "block-reading-001",
            AssetId: asset.AssetId,
            PageId: "page-reading-001",
            PageNumber: 1,
            BlockType: ExtractedBlockType.Question,
            Text: "The manager ____ the report yesterday.",
            Confidence: 0.95m,
            CoordinatesJson: """{"x":10,"y":60,"w":300,"h":30}"""
        ));
        var parser = new FakeReadingDraftParser([
            new ReadingDraftQuestionResult(
                ToeicPart: 5,
                QuestionType: "IncompleteSentence",
                Prompt: "The manager ____ the report yesterday.",
                SkillTags: ["verb_tense", "grammar"],
                PayloadJson: """{"options":{"A":"submit","B":"submitted"},"correctAnswer":"B"}""",
                SourceBlockId: "block-reading-001",
                Confidence: 0.91m
            ),
            new ReadingDraftQuestionResult(
                ToeicPart: 7,
                QuestionType: "ReadingComprehension",
                Prompt: "What is the purpose of the notice?",
                SkillTags: ["main_idea", "notice"],
                PayloadJson: """{"passageId":"passage-001","correctAnswer":"A"}""",
                SourceBlockId: "block-reading-001",
                Confidence: 0.89m
            ),
        ]);
        var handler = new ParseToeicReadingDraftsHandler(repository, parser);

        var result = handler.Handle(new ParseToeicReadingDraftsCommand(asset.AssetId));

        Assert.Equal(2, result.CreatedReadingDraftCount, "Two reading draft questions should persist.");
        Assert.Equal(2, repository.Count("draft_content_items"), "Reading draft rows should persist.");
        var drafts = repository.GetDraftContentItems(asset.AssetId);
        Assert.True(drafts.Any(draft => draft.ToeicPart == 5), "Part 5 draft should persist.");
        Assert.True(drafts.Any(draft => draft.ToeicPart == 7), "Part 7 draft should persist.");
        Assert.True(drafts.All(draft => draft.ItemType == "ReadingQuestion"), "Draft type should identify reading questions.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"schemaVersion\":\"toeic-draft.v1\"", StringComparison.Ordinal)), "Reading draft payload should be versioned.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"kind\":\"ReadingQuestion\"", StringComparison.Ordinal)), "Reading draft payload should declare its canonical kind.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"data\":", StringComparison.Ordinal)), "Reading draft payload should wrap parser data.");
        Assert.True(drafts.Any(draft => draft.PayloadJson.Contains("verb_tense", StringComparison.Ordinal)), "Skill tags should persist in payload.");
        Assert.True(drafts.All(draft => draft.SourceTraceJson.Contains("block-reading-001", StringComparison.Ordinal)), "Source trace should include extracted block.");
    }

    public static void ParsesToeicListeningDraftGroups()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var audioAsset = SeedSourceAsset(repository) with
        {
            AssetId = "asset-part3-audio",
            FileName = "part3-conversation-01.mp3",
            MimeType = "audio/mpeg",
            Extension = ".mp3",
            DetectedRole = SourceAssetRole.Audio,
            ProviderUrl = "https://drive.google.com/file/d/part3-audio",
            ObjectKey = "source-assets/sparta/part3-conversation-01.mp3",
            Checksum = "sha256-part3-audio",
        };
        repository.UpsertSourceAsset(audioAsset);
        var parser = new FakeListeningDraftParser([
            new ListeningDraftQuestionResult(
                ToeicPart: 3,
                GroupId: "part3-conversation-001",
                QuestionNumber: 32,
                Prompt: "What does the woman ask the man to do?",
                SkillTags: ["conversation_purpose", "request"],
                PayloadJson: """{"options":{"A":"Call a client","B":"Send a contract"},"correctAnswer":"B"}""",
                Confidence: 0.9m
            ),
            new ListeningDraftQuestionResult(
                ToeicPart: 3,
                GroupId: "part3-conversation-001",
                QuestionNumber: 33,
                Prompt: "What will the man probably do next?",
                SkillTags: ["inference", "conversation"],
                PayloadJson: """{"options":{"A":"Review a document","B":"Leave the office"},"correctAnswer":"A"}""",
                Confidence: 0.88m
            ),
        ]);
        var handler = new ParseToeicListeningGroupsHandler(repository, parser);

        var result = handler.Handle(new ParseToeicListeningGroupsCommand(audioAsset.AssetId));

        Assert.Equal(2, result.CreatedListeningDraftCount, "Two grouped listening questions should persist.");
        Assert.Equal(1, result.CreatedGroupCount, "One Part 3 group relationship should be counted.");
        Assert.Equal(2, repository.Count("draft_content_items"), "Listening draft rows should persist.");
        var drafts = repository.GetDraftContentItems(audioAsset.AssetId);
        Assert.True(drafts.All(draft => draft.ToeicPart == 3), "Part 3 should persist on listening drafts.");
        Assert.True(drafts.All(draft => draft.ItemType == "ListeningQuestion"), "Draft type should identify listening questions.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"schemaVersion\":\"toeic-draft.v1\"", StringComparison.Ordinal)), "Listening draft payload should be versioned.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"kind\":\"ListeningQuestion\"", StringComparison.Ordinal)), "Listening draft payload should declare its canonical kind.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("\"data\":", StringComparison.Ordinal)), "Listening draft payload should wrap parser data.");
        Assert.True(drafts.All(draft => draft.PayloadJson.Contains("part3-conversation-001", StringComparison.Ordinal)), "Group id should persist in payload.");
        Assert.True(drafts.All(draft => draft.SourceTraceJson.Contains(audioAsset.AssetId, StringComparison.Ordinal)), "Source trace should include audio asset.");
    }

    public static void ValidatesToeicDraftContentAndRecordsIssues()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var asset = SeedSourceAsset(repository);
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-valid-part5",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"schemaVersion":"toeic-draft.v1","kind":"ReadingQuestion","data":{"prompt":"The manager ____ the report.","skillTags":["verb_tense"],"parserPayload":{"options":{"A":"submit","B":"submitted"},"correctAnswer":"B"}}}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-001"}""",
            ParserConfidence: 0.91m,
            Status: DraftContentStatus.PendingValidation
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-unversioned-part5",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"prompt":"The manager ____ the report.","skillTags":["verb_tense"],"parserPayload":{"correctAnswer":"B"}}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-001"}""",
            ParserConfidence: 0.91m,
            Status: DraftContentStatus.PendingValidation
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-missing-answer-options-part5",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"schemaVersion":"toeic-draft.v1","kind":"ReadingQuestion","data":{"prompt":"The manager ____ the report.","skillTags":["verb_tense"],"parserPayload":{"correctAnswer":"B"}}}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-001"}""",
            ParserConfidence: 0.91m,
            Status: DraftContentStatus.PendingValidation
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-missing-structured-prompt-part5",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"schemaVersion":"toeic-draft.v1","kind":"ReadingQuestion","data":{"skillTags":["verb_tense"],"parserPayload":{"promptMetadata":"ocr-row-had-prompt-token","options":{"A":"submit","B":"submitted"},"correctAnswer":"B"}}}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-001"}""",
            ParserConfidence: 0.91m,
            Status: DraftContentStatus.PendingValidation
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-invalid-part7",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 7,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"schemaVersion":"toeic-draft.v1","kind":"ReadingQuestion","data":{"prompt":"What is the purpose?","skillTags":["main_idea"],"parserPayload":{"options":{"A":"To announce a policy","B":"To request feedback"},"correctAnswer":"A"}}}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-001"}""",
            ParserConfidence: 0.9m,
            Status: DraftContentStatus.PendingValidation
        ));
        var handler = new ValidateToeicDraftContentHandler(repository);

        var result = handler.Handle(new ValidateToeicDraftContentCommand(asset.AssetId));

        Assert.Equal(1, result.ValidDraftCount, "Valid draft should pass validation.");
        Assert.Equal(4, result.InvalidDraftCount, "Invalid drafts should fail validation.");
        Assert.Equal(4, repository.Count("validation_issues"), "Invalid drafts should create validation issues.");
        var drafts = repository.GetDraftContentItems(asset.AssetId);
        Assert.Equal(DraftContentStatus.ReadyForReview, drafts.Single(draft => draft.DraftId == "draft-valid-part5").Status, "Valid draft should be ready for review.");
        Assert.Equal(DraftContentStatus.ValidationFailed, drafts.Single(draft => draft.DraftId == "draft-unversioned-part5").Status, "Unversioned draft should be marked failed.");
        Assert.Equal(DraftContentStatus.ValidationFailed, drafts.Single(draft => draft.DraftId == "draft-missing-answer-options-part5").Status, "Question draft without answer options should be marked failed.");
        Assert.Equal(DraftContentStatus.ValidationFailed, drafts.Single(draft => draft.DraftId == "draft-missing-structured-prompt-part5").Status, "Question draft without data.prompt should be marked failed.");
        Assert.Equal(DraftContentStatus.ValidationFailed, drafts.Single(draft => draft.DraftId == "draft-invalid-part7").Status, "Invalid draft should be marked failed.");
    }

    public static void ReviewsAndPublishesApprovedToeicDraftContent()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var asset = SeedSourceAsset(repository);
        repository.UpsertPublishedLesson(new PublishedLesson(
            LessonId: "lesson-part5-word-form",
            UnitId: "part5-word-form",
            ToeicPart: 5,
            Title: "Word Form",
            Objective: "Choose the correct part of speech.",
            SkillTags: "word_form,grammar",
            SourceTraceJson: """{"sourceId":"sheet-row-8"}""",
            Status: PublishedContentStatus.Published
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-review-approved",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"schemaVersion":"toeic-draft.v1","kind":"ReadingQuestion","data":{"prompt":"The manager ____ the report.","skillTags":["verb_tense"],"parserPayload":{"options":{"A":"submit","B":"submitted"},"correctAnswer":"B","explanation":"Past tense is required."}}}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-001"}""",
            ParserConfidence: 0.91m,
            Status: DraftContentStatus.ReadyForReview
        ));
        repository.UpsertDraftContentItem(new DraftContentItem(
            DraftId: "draft-review-rejected",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "ReadingQuestion",
            PayloadJson: """{"prompt":"Bad OCR row","options":{"A":"x"},"correctAnswer":"A","skillTags":["ocr"]}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","sourceBlockId":"block-reading-002"}""",
            ParserConfidence: 0.86m,
            Status: DraftContentStatus.ReadyForReview
        ));
        var handler = new ReviewAndPublishToeicContentHandler(repository);

        var result = handler.Handle(new ReviewAndPublishToeicContentCommand(
            AssetId: asset.AssetId,
            LessonId: "lesson-part5-word-form",
            Decisions:
            [
                new ReviewDecision("draft-review-approved", ReviewDecisionAction.Approve),
                new ReviewDecision("draft-review-rejected", ReviewDecisionAction.Reject),
            ]
        ));

        Assert.Equal(1, result.PublishedCount, "Approved draft should publish.");
        Assert.Equal(1, result.RejectedCount, "Rejected draft should be hidden.");
        Assert.Equal(1, repository.Count("published_questions"), "Only approved draft should become published question.");
        var question = repository.GetPublishedQuestions(5).Single();
        Assert.Equal("published-question-draft-review-approved", question.QuestionId, "Published question id should trace draft id.");
        Assert.Equal("B", question.CorrectAnswer, "Correct answer should publish.");
        var drafts = repository.GetDraftContentItems(asset.AssetId);
        Assert.Equal(DraftContentStatus.Published, drafts.Single(draft => draft.DraftId == "draft-review-approved").Status, "Approved draft should be marked published.");
        Assert.Equal(DraftContentStatus.Rejected, drafts.Single(draft => draft.DraftId == "draft-review-rejected").Status, "Rejected draft should be marked rejected.");
    }

    public static void OnboardsLearnerAndReturnsNextPlacementAction()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var handler = new OnboardLearnerHandler(repository);

        var result = handler.Handle(new OnboardLearnerCommand(
            LearnerId: "learner-onboarding-001",
            DisplayName: "Nguyen Van A",
            Email: "learner@example.com",
            TargetScore: 850,
            CurrentEstimatedScore: 520,
            DailyStudyMinutes: 75,
            TimeZoneId: "Asia/Ho_Chi_Minh"
        ));

        var profile = repository.GetLearnerProfile("learner-onboarding-001");
        Assert.True(profile is not null, "Onboarding must persist learner profile.");
        if (profile is null) return;

        Assert.Equal(850, profile.TargetScore, "Target score should persist from onboarding.");
        Assert.Equal(520, profile.CurrentEstimatedScore, "Current estimate should persist from onboarding.");
        Assert.Equal(75, profile.DailyStudyMinutes, "Daily study minutes should persist from onboarding.");
        Assert.Equal(LearnerProfileStatus.Active, profile.Status, "Onboarded learner should be active.");
        Assert.Equal("StartPlacement", result.NextAction.Code, "New learner should be sent to placement after onboarding.");
        Assert.Equal("/api/learner/placement/start", result.NextAction.ApiRoute, "Next action should point to backend-owned placement start.");
        Assert.True(
            ApiContractCatalog.All.Any(contract =>
                contract.Method == "POST"
                && contract.Route == "/api/learner/onboarding"
                && contract.Audience == ApiAudience.Learner
                && contract.ResponseContract == "OnboardLearnerResponse"),
            "Learner onboarding route contract must be typed."
        );

        handler.Handle(new OnboardLearnerCommand(
            LearnerId: "learner-onboarding-001",
            DisplayName: "Nguyen Van A",
            Email: "learner@example.com",
            TargetScore: 900,
            CurrentEstimatedScore: 610,
            DailyStudyMinutes: 90,
            TimeZoneId: "Asia/Ho_Chi_Minh"
        ));

        Assert.Equal(1, repository.Count("learner_profiles"), "Repeat onboarding should update profile idempotently.");
        Assert.Equal(900, repository.GetLearnerProfile("learner-onboarding-001")!.TargetScore, "Repeat onboarding should update goal.");
    }

    public static void LearnerHomeReadsPersistedProfileAfterRestart()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"toeic-learner-home-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            using (var repository = SqliteKnowledgeRepository.FromConnectionString(connectionString))
            {
                repository.Initialize();
                new OnboardLearnerHandler(repository).Handle(new OnboardLearnerCommand(
                    LearnerId: "learner-home-001",
                    DisplayName: "Nguyen Van B",
                    Email: "home@example.com",
                    TargetScore: 800,
                    CurrentEstimatedScore: 470,
                    DailyStudyMinutes: 60,
                    TimeZoneId: "Asia/Ho_Chi_Minh"
                ));
            }

            using (var restartedRepository = SqliteKnowledgeRepository.FromConnectionString(connectionString))
            {
                restartedRepository.Initialize();
                var home = new GetLearnerHomeHandler(restartedRepository).Handle(new GetLearnerHomeQuery("learner-home-001"));

                Assert.Equal("learner-home-001", home.LearnerId, "Learner home must come from persisted profile.");
                Assert.Equal("placement", home.CurrentUnitId, "Persisted learner without placement should be routed to placement.");
                Assert.Equal("Placement", home.NextActivity.ActivityType, "Next activity should be placement, not demo lesson.");
                Assert.Equal("toeic-placement-start", home.NextActivity.ActivityId, "Home CTA should be backend-owned placement start.");
                Assert.Equal(0, home.ReviewCount, "New persisted profile should start with no review blockers.");
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    public static void StartsPlacementSessionAndResumesActiveDuplicate()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        new OnboardLearnerHandler(repository).Handle(new OnboardLearnerCommand(
            LearnerId: "learner-placement-001",
            DisplayName: "Nguyen Van C",
            Email: "placement@example.com",
            TargetScore: 820,
            CurrentEstimatedScore: 510,
            DailyStudyMinutes: 80,
            TimeZoneId: "Asia/Ho_Chi_Minh"
        ));
        var handler = new StartPlacementSessionHandler(repository);

        var first = handler.Handle(new StartPlacementSessionCommand("learner-placement-001"));
        var duplicate = handler.Handle(new StartPlacementSessionCommand("learner-placement-001"));

        Assert.Equal(first.SessionId, duplicate.SessionId, "Duplicate active placement start should resume existing session.");
        Assert.Equal(PlacementSessionStatus.InProgress, first.Status, "Started placement should be in progress.");
        Assert.Equal("ResumePlacement", duplicate.NextAction.Code, "Duplicate start should return resume action.");
        Assert.Equal(1, repository.Count("placement_sessions"), "Active duplicate must not create a second session.");
        Assert.Equal(1, repository.GetPlacementSessions("learner-placement-001").Count, "Placement session should persist in repository.");
        Assert.True(
            ApiContractCatalog.All.Any(contract =>
                contract.Method == "POST"
                && contract.Route == "/api/learner/placement/start"
                && contract.Audience == ApiAudience.Learner
                && contract.ResponseContract == "StartPlacementSessionResponse"),
            "Placement start route contract must be typed."
        );
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

    public static void PostgresMigrationFoundationIsExplicit()
    {
        var migrations = PostgresMigrationCatalog.All;

        Assert.True(migrations.Count > 0, "PostgreSQL migration catalog must not be empty.");
        Assert.Equal("postgresql", PostgresMigrationCatalog.Provider, "Migration provider must be PostgreSQL.");
        Assert.Equal(
            "001_platform_schema_history",
            migrations[0].Id,
            "First migration must create the platform schema history."
        );
        Assert.True(
            migrations[0].SqlStatements.Contains("CREATE TABLE IF NOT EXISTS platform_schema_history", StringComparison.Ordinal),
            "First migration must create schema history table."
        );
        Assert.False(
            migrations.Any(migration => migration.SqlStatements.Contains("sqlite", StringComparison.OrdinalIgnoreCase)),
            "Production migrations must not use SQLite syntax."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "002_source_assets"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS source_containers", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS source_assets", StringComparison.Ordinal)),
            "Source asset migration must create source container and asset tables."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "003_extracted_content"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS extracted_pages", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS extracted_text_blocks", StringComparison.Ordinal)),
            "Extracted content migration must create extracted page and text block tables."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "004_draft_content"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS draft_content_items", StringComparison.Ordinal)),
            "Draft content migration must create draft content table."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "005_published_lessons"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS published_lessons", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS guided_examples", StringComparison.Ordinal)),
            "Published lesson migration must create lesson and guided example tables."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "006_published_questions"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS published_questions", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_published_questions_part_status", StringComparison.Ordinal)),
            "Published question migration must create question table and lookup index."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "007_published_tests"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS published_tests", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS published_test_sections", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS published_test_items", StringComparison.Ordinal)),
            "Published test migration must create test, section, and ordered item tables."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "008_learner_profiles"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS learner_profiles", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_learner_profiles_status", StringComparison.Ordinal)),
            "Learner profile migration must create profile table and status index."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "009_learner_assignments_attempts"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS learner_assignments", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS activity_sessions", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS learner_attempts", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS attempt_answers", StringComparison.Ordinal)),
            "Learner assignment migration must create lifecycle tables."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "010_review_mastery_records"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS review_items", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS repair_attempts", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS mastery_records", StringComparison.Ordinal)),
            "Review and mastery migration must create review, repair, and mastery tables."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "011_toeic_data_integrity"
                && migration.SqlStatements.Contains("idx_review_items_blocking_unlock", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_attempt_answers_question", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_published_questions_media", StringComparison.Ordinal)),
            "Integrity migration must add production lookup indexes."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "012_source_discovery_issues"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS source_discovery_issues", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_source_discovery_issues_source_status", StringComparison.Ordinal)),
            "Source discovery issue migration must create issue table and lookup index."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "013_source_resolution_records"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS source_resolution_records", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_source_resolution_records_source_status", StringComparison.Ordinal)),
            "Source resolution migration must create resolution table and lookup index."
        );
        Assert.True(
            migrations.Any(migration =>
                migration.Id == "014_source_audio_metadata"
                && migration.SqlStatements.Contains("CREATE TABLE IF NOT EXISTS source_audio_metadata", StringComparison.Ordinal)
                && migration.SqlStatements.Contains("idx_source_audio_metadata_asset", StringComparison.Ordinal)),
            "Audio metadata migration must create metadata table and asset index."
        );
    }

    public static void ObjectStorageTestDoubleStoresListsAndDeletesObjects()
    {
        IObjectStorage storage = new InMemoryObjectStorage();
        var objectKey = new ObjectKey("source-assets/audio/part2-track01.mp3");
        var payload = Encoding.UTF8.GetBytes("audio-bytes");

        storage.Put(new PutObjectRequest(objectKey, "audio/mpeg", payload));

        var stored = storage.Get(objectKey);
        Assert.True(stored is not null, "Stored object must be readable.");
        if (stored is null) return;

        Assert.Equal("audio/mpeg", stored.ContentType, "Content type must round-trip.");
        Assert.Equal("audio-bytes", Encoding.UTF8.GetString(stored.Content), "Payload must round-trip.");
        Assert.Contains(storage.List("source-assets/audio"), objectKey.Value);

        storage.Delete(objectKey);

        Assert.True(storage.Get(objectKey) is null, "Deleted object must not be readable.");
        Assert.False(storage.List("source-assets/audio").Contains(objectKey.Value), "Deleted object must leave list results.");
    }

    public static void BackgroundJobQueueRetriesThenRecordsFailure()
    {
        IBackgroundJobQueue queue = new InMemoryBackgroundJobQueue(new BackgroundJobRetryPolicy(maxAttempts: 2));
        var jobId = queue.Enqueue(new EnqueueBackgroundJobRequest("extract-pdf", "source-asset-1"));

        var firstLease = queue.TryLeaseNext();
        Assert.True(firstLease is not null, "Queued job should be leased.");
        if (firstLease is null) return;

        Assert.Equal(jobId, firstLease.Job.JobId, "Lease should return queued job.");
        queue.RecordFailure(firstLease.Job.JobId, "PDF parser timed out.");

        var retryLease = queue.TryLeaseNext();
        Assert.True(retryLease is not null, "Failed job under retry limit should be leased again.");
        if (retryLease is null) return;

        Assert.Equal(2, retryLease.Job.AttemptCount, "Retry lease should increment attempt count.");
        queue.RecordFailure(retryLease.Job.JobId, "PDF parser timed out again.");

        var finalJob = queue.Get(jobId);
        Assert.Equal(BackgroundJobStatus.Failed, finalJob.Status, "Job should fail after max attempts.");
        Assert.Equal("PDF parser timed out again.", finalJob.FailureReason, "Failure reason should be recorded.");
        Assert.True(queue.TryLeaseNext() is null, "Failed job must not be leased again.");
    }

    public static void ApiContractCatalogDefinesStableTypedRoutes()
    {
        var contracts = ApiContractCatalog.All;

        Assert.True(contracts.Count > 0, "API contract catalog must not be empty.");
        Assert.Equal("2026-07-01", ApiContractCatalog.Version, "API contract version must be explicit.");
        Assert.True(
            contracts.Any(contract =>
                contract.Method == "GET"
                && contract.Route == "/api/learner/home"
                && contract.Audience == ApiAudience.Learner
                && contract.ResponseContract == "LearnerHomeResponse"),
            "Learner home route contract must be typed."
        );
        Assert.True(
            contracts.Any(contract =>
                contract.Method == "POST"
                && contract.Route == "/api/source-manifest/toeic-audit"
                && contract.Audience == ApiAudience.Admin
                && contract.ResponseContract == "ImportToeicSourceManifestResult"),
            "Source manifest import route contract must be typed."
        );
        Assert.True(
            contracts.Any(contract =>
                contract.Method == "POST"
                && contract.Route == "/api/source-manifest/local-downloads"
                && contract.Audience == ApiAudience.Admin
                && contract.ResponseContract == "ImportLocalToeicDownloadsResult"),
            "Local downloads import route contract must be typed."
        );
        Assert.True(
            contracts.Any(contract =>
                contract.Method == "GET"
                && contract.Route == "/api/admin/content-coverage"
                && contract.Audience == ApiAudience.Admin
                && contract.ResponseContract == "ContentCoverageSnapshot"),
            "Content coverage route contract must be typed."
        );

        var duplicateRoute = contracts
            .GroupBy(contract => $"{contract.Method} {contract.Route}", StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        Assert.True(duplicateRoute is null, "API contract catalog must not contain duplicate method+route entries.");
    }

    public static void PlatformHealthReportsDependencyReadiness()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var storage = new InMemoryObjectStorage();
        var queue = new InMemoryBackgroundJobQueue(new BackgroundJobRetryPolicy(maxAttempts: 2));
        IPlatformHealthService healthService = new PlatformHealthService(repository, storage, queue);

        var snapshot = healthService.Check();

        Assert.Equal(PlatformHealthStatus.Healthy, snapshot.Status, "Platform health should be healthy.");
        Assert.True(snapshot.Dependencies.Count >= 3, "Health snapshot should include DB, storage, and job queue.");
        Assert.True(
            snapshot.Dependencies.All(dependency => dependency.Status == PlatformHealthStatus.Healthy),
            "All configured dependencies should be healthy."
        );
    }

    public static void RepositoryPersistsSourceContainersAndAssetsIdempotently()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var source = SourceManifestClassifier.Classify(
            7,
            "SPARTA TOEIC ( quyển hồng - 10TEST )",
            "https://drive.google.com/drive/folders/example",
            inaccessible: false,
            hasPdf: true,
            hasAudio: true,
            hasTranscript: true,
            hasAnswerKey: true,
            hasImage: false
        );
        repository.UpsertSourceManifestEntry(source);
        var container = new SourceContainer(
            ContainerId: "container-drive-sparta",
            SourceId: source.SourceId,
            Provider: SourceProvider.GoogleDrive,
            ExternalId: "drive-folder-example",
            Title: "SPARTA TOEIC source folder",
            AccessStatus: SourceAccessStatus.Accessible,
            DiscoveredAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        );
        var asset = new SourceAsset(
            AssetId: "asset-sparta-test-01-audio",
            ContainerId: container.ContainerId,
            SourceId: source.SourceId,
            FileName: "test-01.mp3",
            MimeType: "audio/mpeg",
            Extension: ".mp3",
            SizeBytes: 345_000,
            DetectedRole: SourceAssetRole.Audio,
            ProviderUrl: "https://drive.google.com/file/d/audio",
            ObjectKey: "source-assets/sparta/test-01.mp3",
            Checksum: "sha256-audio"
        );

        repository.UpsertSourceContainer(container);
        repository.UpsertSourceContainer(container with { Title = "SPARTA TOEIC source folder updated" });
        repository.UpsertSourceAsset(asset);
        repository.UpsertSourceAsset(asset with { SizeBytes = 346_000 });

        var containers = repository.GetSourceContainers(source.SourceId);
        var assets = repository.GetSourceAssets(container.ContainerId);

        Assert.Equal(1, repository.Count("source_containers"), "Container upsert must be idempotent.");
        Assert.Equal(1, repository.Count("source_assets"), "Asset upsert must be idempotent.");
        Assert.Equal("SPARTA TOEIC source folder updated", containers.Single().Title, "Container update should persist.");
        Assert.Equal(SourceAssetRole.Audio, assets.Single().DetectedRole, "Asset role should persist.");
        Assert.Equal(346_000L, assets.Single().SizeBytes, "Asset update should persist.");
        Assert.Equal("sha256-audio", assets.Single().Checksum, "Asset checksum should persist.");
    }

    public static void RepositoryPersistsExtractedPagesAndTextBlocksIdempotently()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var asset = SeedSourceAsset(repository);
        var page = new ExtractedPage(
            PageId: "page-sparta-001",
            AssetId: asset.AssetId,
            PageNumber: 1,
            Width: 595,
            Height: 842,
            ExtractedAtUtc: new DateTimeOffset(2026, 7, 2, 1, 0, 0, TimeSpan.Zero)
        );
        var block = new ExtractedTextBlock(
            BlockId: "block-sparta-001",
            AssetId: asset.AssetId,
            PageId: page.PageId,
            PageNumber: 1,
            BlockType: ExtractedBlockType.Question,
            Text: "The manager ____ the report yesterday.",
            Confidence: 0.94m,
            CoordinatesJson: """{"x":10,"y":20,"w":300,"h":40}"""
        );

        repository.UpsertExtractedPage(page);
        repository.UpsertExtractedPage(page with { Width = 596 });
        repository.UpsertExtractedTextBlock(block);
        repository.UpsertExtractedTextBlock(block with { Confidence = 0.95m });

        var pages = repository.GetExtractedPages(asset.AssetId);
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);

        Assert.Equal(1, repository.Count("extracted_pages"), "Page upsert must be idempotent.");
        Assert.Equal(1, repository.Count("extracted_text_blocks"), "Block upsert must be idempotent.");
        Assert.Equal(596, pages.Single().Width, "Page update should persist.");
        Assert.Equal(ExtractedBlockType.Question, blocks.Single().BlockType, "Block type should persist.");
        Assert.Equal(0.95m, blocks.Single().Confidence, "Block confidence should persist.");
        Assert.Equal(page.PageId, blocks.Single().PageId, "Block should reference extracted page.");
    }

    private static SourceAsset SeedSourceAsset(SqliteKnowledgeRepository repository)
    {
        var source = SourceManifestClassifier.Classify(
            8,
            "SPARTA TOEIC PDF",
            "https://drive.google.com/drive/folders/example-pdf",
            inaccessible: false,
            hasPdf: true,
            hasAudio: false,
            hasTranscript: false,
            hasAnswerKey: true,
            hasImage: false
        );
        repository.UpsertSourceManifestEntry(source);
        var container = new SourceContainer(
            "container-drive-sparta-pdf",
            source.SourceId,
            SourceProvider.GoogleDrive,
            "drive-folder-example-pdf",
            "SPARTA PDF folder",
            SourceAccessStatus.Accessible,
            new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        );
        var asset = new SourceAsset(
            "asset-sparta-test-01-pdf",
            container.ContainerId,
            source.SourceId,
            "test-01.pdf",
            "application/pdf",
            ".pdf",
            1_200_000,
            SourceAssetRole.Pdf,
            "https://drive.google.com/file/d/pdf",
            "source-assets/sparta/test-01.pdf",
            "sha256-pdf"
        );
        repository.UpsertSourceContainer(container);
        repository.UpsertSourceAsset(asset);
        return asset;
    }

    public static void RepositoryPersistsDraftContentSafelyAwayFromLearnerContracts()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var asset = SeedSourceAsset(repository);
        var draft = new DraftContentItem(
            DraftId: "draft-part5-001",
            AssetId: asset.AssetId,
            MaterialClass: MaterialClass.TestBook,
            ToeicPart: 5,
            ItemType: "Question",
            PayloadJson: """{"prompt":"The manager ____ the report yesterday."}""",
            SourceTraceJson: """{"assetId":"asset-sparta-test-01-pdf","page":1,"blockId":"block-sparta-001"}""",
            ParserConfidence: 0.88m,
            Status: DraftContentStatus.PendingValidation
        );

        repository.UpsertDraftContentItem(draft);
        repository.UpsertDraftContentItem(draft with
        {
            ParserConfidence = 0.91m,
            Status = DraftContentStatus.ReadyForReview,
        });

        var drafts = repository.GetDraftContentItems(asset.AssetId);
        var learnerContracts = ApiContractCatalog.All
            .Where(contract => contract.Audience == ApiAudience.Learner)
            .Select(contract => contract.ResponseContract);

        Assert.Equal(1, repository.Count("draft_content_items"), "Draft upsert must be idempotent.");
        Assert.Equal(DraftContentStatus.ReadyForReview, drafts.Single().Status, "Draft status update should persist.");
        Assert.Equal(0.91m, drafts.Single().ParserConfidence, "Parser confidence update should persist.");
        Assert.True(drafts.Single().SourceTraceJson.Contains("block-sparta-001", StringComparison.Ordinal), "Source trace should persist.");
        Assert.False(
            learnerContracts.Any(contract => contract.Contains("Draft", StringComparison.OrdinalIgnoreCase)),
            "Learner API contracts must not expose draft content."
        );
    }

    public static void RepositoryPersistsPublishedLessonsAndGuidedExamples()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var lesson = new PublishedLesson(
            LessonId: "lesson-part5-word-form",
            UnitId: "part5-word-form",
            ToeicPart: 5,
            Title: "Word Form",
            Objective: "Choose the correct part of speech from sentence position.",
            SkillTags: "word_form,grammar",
            SourceTraceJson: """{"sourceId":"sheet-row-8","draftId":"draft-part5-001"}""",
            Status: PublishedContentStatus.Published
        );
        var example = new GuidedExample(
            ExampleId: "example-part5-word-form-001",
            LessonId: lesson.LessonId,
            Prompt: "The marketing team needs a more ____ strategy.",
            Explanation: "Before strategy, the blank needs an adjective: effective.",
            DisplayOrder: 1
        );

        repository.UpsertPublishedLesson(lesson);
        repository.UpsertPublishedLesson(lesson with { Title = "Word Form Foundations" });
        repository.UpsertGuidedExample(example);
        repository.UpsertGuidedExample(example with { Explanation = "The blank before a noun usually needs an adjective." });

        var lessons = repository.GetPublishedLessons("part5-word-form");
        var examples = repository.GetGuidedExamples(lesson.LessonId);

        Assert.Equal(1, repository.Count("published_lessons"), "Lesson upsert must be idempotent.");
        Assert.Equal(1, repository.Count("guided_examples"), "Guided example upsert must be idempotent.");
        Assert.Equal("Word Form Foundations", lessons.Single().Title, "Lesson update should persist.");
        Assert.Equal("word_form,grammar", lessons.Single().SkillTags, "Skill tags should persist.");
        Assert.Equal(1, examples.Single().DisplayOrder, "Guided example order should persist.");
        Assert.True(examples.Single().Explanation.Contains("adjective", StringComparison.Ordinal), "Guided explanation should persist.");
    }

    public static void RepositoryPersistsPublishedQuestionsAndEnforcesPartRules()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        repository.UpsertPublishedLesson(new PublishedLesson(
            LessonId: "lesson-part5-word-form",
            UnitId: "part5-word-form",
            ToeicPart: 5,
            Title: "Word Form Foundations",
            Objective: "Choose the correct part of speech from sentence position.",
            SkillTags: "word_form,grammar",
            SourceTraceJson: """{"sourceId":"sheet-row-8"}""",
            Status: PublishedContentStatus.Published
        ));
        var part5Question = new PublishedQuestion(
            QuestionId: "question-part5-word-form-001",
            LessonId: "lesson-part5-word-form",
            ToeicPart: 5,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "The marketing team needs a more ____ strategy.",
            OptionsJson: """{"A":"effect","B":"effective","C":"effectively","D":"effectiveness"}""",
            CorrectAnswer: "B",
            Explanation: "Before strategy, the blank needs an adjective.",
            MediaAssetId: null,
            PassageId: null,
            GroupId: null,
            EvidenceJson: """{"sourceId":"sheet-row-8","blockId":"block-sparta-001"}""",
            SkillTags: "word_form,grammar",
            SourceTraceJson: """{"draftId":"draft-part5-001"}""",
            Status: PublishedContentStatus.Published
        );

        repository.UpsertPublishedQuestion(part5Question);
        repository.UpsertPublishedQuestion(part5Question with { Explanation = "A noun phrase needs the adjective effective." });

        var questions = repository.GetPublishedQuestions(5);

        Assert.Equal(1, repository.Count("published_questions"), "Published question upsert must be idempotent.");
        Assert.Equal("question-part5-word-form-001", questions.Single().QuestionId, "Question id should persist.");
        Assert.Equal(PublishedQuestionType.SingleQuestion, questions.Single().QuestionType, "Question type should persist.");
        Assert.True(questions.Single().Explanation.Contains("adjective", StringComparison.Ordinal), "Updated explanation should persist.");
        Assert.Throws<InvalidOperationException>(
            () => repository.UpsertPublishedQuestion(part5Question with
            {
                QuestionId = "invalid-part1-no-media",
                ToeicPart = 1,
                MediaAssetId = null,
            }),
            "Part 1 question must require media."
        );
        Assert.Throws<InvalidOperationException>(
            () => repository.UpsertPublishedQuestion(part5Question with
            {
                QuestionId = "invalid-part7-no-passage",
                ToeicPart = 7,
                PassageId = null,
            }),
            "Part 7 question must require passage context."
        );
    }

    public static void RepositoryPersistsToeicTestsSectionsAndOrderedItems()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var test = new PublishedTest(
            TestId: "test-full-toeic-001",
            TestMode: PublishedTestMode.Full,
            Title: "Full TOEIC Practice Test 01",
            TargetQuestionCount: 200,
            DurationMinutes: 120,
            SourceTraceJson: """{"sourceId":"sheet-row-7"}""",
            Status: PublishedContentStatus.Published
        );
        var listening = new PublishedTestSection(
            SectionId: "section-full-001-listening",
            TestId: test.TestId,
            SectionType: ToeicTestSectionType.Listening,
            DisplayOrder: 1,
            TargetQuestionCount: 100,
            DurationMinutes: 45
        );
        var reading = listening with
        {
            SectionId = "section-full-001-reading",
            SectionType = ToeicTestSectionType.Reading,
            DisplayOrder = 2,
            TargetQuestionCount = 100,
            DurationMinutes = 75,
        };
        var firstItem = new PublishedTestItem(
            TestItemId: "test-item-full-001-001",
            SectionId: listening.SectionId,
            QuestionId: "question-part1-photo-001",
            ToeicPart: 1,
            DisplayOrder: 1,
            ScoreWeight: 1
        );
        var secondItem = firstItem with
        {
            TestItemId = "test-item-full-001-002",
            QuestionId = "question-part1-photo-002",
            DisplayOrder = 2,
        };

        repository.UpsertPublishedTest(test);
        repository.UpsertPublishedTest(test with { Title = "Full TOEIC Practice Test 01 - Revised" });
        repository.UpsertPublishedTestSection(reading);
        repository.UpsertPublishedTestSection(listening);
        repository.UpsertPublishedTestItem(secondItem);
        repository.UpsertPublishedTestItem(firstItem);

        var tests = repository.GetPublishedTests(PublishedTestMode.Full);
        var sections = repository.GetPublishedTestSections(test.TestId);
        var items = repository.GetPublishedTestItems(listening.SectionId);

        Assert.Equal(1, repository.Count("published_tests"), "Published test upsert must be idempotent.");
        Assert.Equal(2, repository.Count("published_test_sections"), "Full test must represent Listening and Reading sections.");
        Assert.Equal(2, repository.Count("published_test_items"), "Ordered test items should persist.");
        Assert.Equal("Full TOEIC Practice Test 01 - Revised", tests.Single().Title, "Published test update should persist.");
        Assert.Equal(200, tests.Single().TargetQuestionCount, "Full TOEIC question count should be representable.");
        Assert.Equal(ToeicTestSectionType.Listening, sections[0].SectionType, "Sections should sort by display order.");
        Assert.Equal(ToeicTestSectionType.Reading, sections[1].SectionType, "Reading section should sort after Listening.");
        Assert.Equal("question-part1-photo-001", items[0].QuestionId, "Items should sort by display order.");
        Assert.Equal("question-part1-photo-002", items[1].QuestionId, "Second ordered item should persist.");
        Assert.Throws<InvalidOperationException>(
            () => repository.UpsertPublishedTest(test with { TestId = "invalid-full-test", TargetQuestionCount = 199 }),
            "Full TOEIC tests must represent 200 questions."
        );
    }

    public static void RepositoryPersistsLearnerProfilesAcrossRestart()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"toeic-learner-profile-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            using (var repository = SqliteKnowledgeRepository.FromConnectionString(connectionString))
            {
                repository.Initialize();
                repository.UpsertLearnerProfile(new LearnerProfile(
                    LearnerId: "learner-production-001",
                    DisplayName: "Nguyen Van A",
                    Email: "learner@example.com",
                    TargetScore: 850,
                    CurrentEstimatedScore: 620,
                    DailyStudyMinutes: 60,
                    TimeZoneId: "Asia/Ho_Chi_Minh",
                    Status: LearnerProfileStatus.Active,
                    CreatedAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero),
                    UpdatedAtUtc: new DateTimeOffset(2026, 7, 2, 1, 0, 0, TimeSpan.Zero)
                ));
                repository.UpsertLearnerProfile(repository.GetLearnerProfile("learner-production-001")! with
                {
                    CurrentEstimatedScore = 650,
                    DailyStudyMinutes = 75,
                });

                Assert.Equal(1, repository.Count("learner_profiles"), "Learner profile upsert must be idempotent.");
            }

            using (var restartedRepository = SqliteKnowledgeRepository.FromConnectionString(connectionString))
            {
                restartedRepository.Initialize();
                var profile = restartedRepository.GetLearnerProfile("learner-production-001");

                Assert.True(profile is not null, "Learner profile must survive repository restart.");
                if (profile is null) return;

                Assert.Equal(850, profile.TargetScore, "Target TOEIC score should persist.");
                Assert.Equal(650, profile.CurrentEstimatedScore, "Estimated score update should persist.");
                Assert.Equal(75, profile.DailyStudyMinutes, "Daily study goal update should persist.");
                Assert.Equal("Asia/Ho_Chi_Minh", profile.TimeZoneId, "Learner timezone should persist.");
                Assert.Equal(LearnerProfileStatus.Active, profile.Status, "Profile status should persist.");
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    public static void RepositoryPersistsLearnerAssignmentsSessionsAttemptsAndAnswers()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        SeedLearnerProfile(repository);
        var assignment = new LearnerAssignment(
            AssignmentId: "assignment-learner-001-today-plan",
            LearnerId: "learner-production-001",
            AssignmentType: LearnerAssignmentType.MiniTest,
            ContentRefId: "test-full-toeic-001",
            Status: LearnerAssignmentStatus.Assigned,
            AssignedAtUtc: new DateTimeOffset(2026, 7, 2, 2, 0, 0, TimeSpan.Zero),
            DueAtUtc: new DateTimeOffset(2026, 7, 3, 2, 0, 0, TimeSpan.Zero)
        );
        var session = new ActivitySession(
            SessionId: "session-learner-001-mini-test",
            AssignmentId: assignment.AssignmentId,
            LearnerId: assignment.LearnerId,
            ActivityType: LearnerAssignmentType.MiniTest,
            Status: ActivitySessionStatus.InProgress,
            StartedAtUtc: new DateTimeOffset(2026, 7, 2, 2, 5, 0, TimeSpan.Zero),
            CompletedAtUtc: null
        );
        var attempt = new LearnerAttempt(
            AttemptId: "attempt-learner-001-mini-test-001",
            SessionId: session.SessionId,
            LearnerId: assignment.LearnerId,
            Status: LearnerAttemptStatus.Submitted,
            CorrectCount: 8,
            TotalCount: 10,
            ScorePercent: 80,
            SubmittedAtUtc: new DateTimeOffset(2026, 7, 2, 2, 20, 0, TimeSpan.Zero)
        );
        var answer = new AttemptAnswer(
            AnswerId: "answer-attempt-001-q001",
            AttemptId: attempt.AttemptId,
            QuestionId: "question-part5-word-form-001",
            LearnerAnswer: "B",
            CorrectAnswer: "B",
            IsCorrect: true,
            AnsweredAtUtc: new DateTimeOffset(2026, 7, 2, 2, 19, 0, TimeSpan.Zero)
        );

        repository.UpsertLearnerAssignment(assignment);
        repository.UpsertLearnerAssignment(assignment with { Status = LearnerAssignmentStatus.Started });
        repository.UpsertActivitySession(session);
        repository.UpsertActivitySession(session with
        {
            Status = ActivitySessionStatus.Completed,
            CompletedAtUtc = new DateTimeOffset(2026, 7, 2, 2, 20, 0, TimeSpan.Zero),
        });
        repository.UpsertLearnerAttempt(attempt);
        repository.UpsertAttemptAnswer(answer);

        var assignments = repository.GetLearnerAssignments(assignment.LearnerId);
        var sessions = repository.GetActivitySessions(assignment.AssignmentId);
        var attempts = repository.GetLearnerAttempts(session.SessionId);
        var answers = repository.GetAttemptAnswers(attempt.AttemptId);

        Assert.Equal(1, repository.Count("learner_assignments"), "Assignment upsert must be idempotent.");
        Assert.Equal(1, repository.Count("activity_sessions"), "Activity session upsert must be idempotent.");
        Assert.Equal(1, repository.Count("learner_attempts"), "Attempt should persist.");
        Assert.Equal(1, repository.Count("attempt_answers"), "Attempt answer should persist.");
        Assert.Equal(LearnerAssignmentStatus.Started, assignments.Single().Status, "Assignment lifecycle update should persist.");
        Assert.Equal(ActivitySessionStatus.Completed, sessions.Single().Status, "Session completion should persist.");
        Assert.Equal(80, attempts.Single().ScorePercent, "Attempt score should persist.");
        Assert.True(answers.Single().IsCorrect, "Answer correctness should persist.");
        Assert.Throws<InvalidOperationException>(
            () => repository.UpsertLearnerAttempt(attempt with { AttemptId = "invalid-score", CorrectCount = 11 }),
            "Attempt correct count cannot exceed total count."
        );
    }

    private static void SeedLearnerProfile(SqliteKnowledgeRepository repository)
    {
        repository.UpsertLearnerProfile(new LearnerProfile(
            LearnerId: "learner-production-001",
            DisplayName: "Nguyen Van A",
            Email: "learner@example.com",
            TargetScore: 850,
            CurrentEstimatedScore: 650,
            DailyStudyMinutes: 75,
            TimeZoneId: "Asia/Ho_Chi_Minh",
            Status: LearnerProfileStatus.Active,
            CreatedAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc: new DateTimeOffset(2026, 7, 2, 1, 0, 0, TimeSpan.Zero)
        ));
    }

    public static void RepositoryPersistsReviewAndMasteryRecords()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        SeedLearnerProfile(repository);
        var review = new ReviewItem(
            ReviewItemId: "review-learner-001-question-001",
            LearnerId: "learner-production-001",
            SourceAttemptId: "attempt-learner-001-mini-test-001",
            QuestionId: "question-part5-word-form-001",
            UnitId: "part5-word-form",
            ErrorTag: "word_form",
            LearnerAnswer: "A",
            CorrectAnswer: "B",
            Status: ReviewItemStatus.Open,
            IsBlocking: true,
            CreatedAtUtc: new DateTimeOffset(2026, 7, 2, 3, 0, 0, TimeSpan.Zero),
            ResolvedAtUtc: null
        );
        var repair = new RepairAttempt(
            RepairAttemptId: "repair-review-001",
            ReviewItemId: review.ReviewItemId,
            LearnerId: review.LearnerId,
            Answer: "B",
            IsCorrect: true,
            AttemptedAtUtc: new DateTimeOffset(2026, 7, 2, 3, 10, 0, TimeSpan.Zero)
        );
        var mastery = new MasteryRecord(
            MasteryRecordId: "mastery-learner-001-part5-word-form",
            LearnerId: review.LearnerId,
            UnitId: review.UnitId,
            MasteryPercent: 82,
            IsUnlocked: true,
            BlockingReviewCount: 0,
            UpdatedAtUtc: new DateTimeOffset(2026, 7, 2, 3, 15, 0, TimeSpan.Zero)
        );

        repository.UpsertReviewItem(review);
        repository.UpsertReviewItem(review with
        {
            Status = ReviewItemStatus.Resolved,
            IsBlocking = false,
            ResolvedAtUtc = new DateTimeOffset(2026, 7, 2, 3, 12, 0, TimeSpan.Zero),
        });
        repository.UpsertRepairAttempt(repair);
        repository.UpsertMasteryRecord(mastery);

        var reviews = repository.GetReviewItems(review.LearnerId);
        var repairs = repository.GetRepairAttempts(review.ReviewItemId);
        var masteryRecord = repository.GetMasteryRecord(review.LearnerId, review.UnitId);

        Assert.Equal(1, repository.Count("review_items"), "Review item upsert must be idempotent.");
        Assert.Equal(1, repository.Count("repair_attempts"), "Repair attempt should persist.");
        Assert.Equal(1, repository.Count("mastery_records"), "Mastery record should persist.");
        Assert.Equal(ReviewItemStatus.Resolved, reviews.Single().Status, "Review status update should persist.");
        Assert.False(reviews.Single().IsBlocking, "Resolved review should stop blocking.");
        Assert.True(repairs.Single().IsCorrect, "Repair correctness should persist.");
        Assert.True(masteryRecord is not null, "Mastery record should be queryable by learner and unit.");
        if (masteryRecord is null) return;

        Assert.Equal(82, masteryRecord.MasteryPercent, "Mastery percent should persist.");
        Assert.Equal(0, masteryRecord.BlockingReviewCount, "Blocking review count should persist.");
        Assert.Throws<InvalidOperationException>(
            () => repository.UpsertMasteryRecord(mastery with { MasteryRecordId = "invalid-mastery", MasteryPercent = 101 }),
            "Mastery percent must stay within 0-100."
        );
    }

    public static void PerformanceBaselineGetLearnerTodayPlan()
    {
        Console.WriteLine("--- BENCHMARK: GetLearnerTodayPlanHandler ---");
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = "benchmark-learner";
        var pathId = "benchmark-path";

        repository.UpsertLearnerProfile(new LearnerProfile(
            learnerId,
            "Benchmark Learner",
            "benchmark@test.com",
            500,
            0,
            30,
            "UTC",
            LearnerProfileStatus.Active,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));

        repository.UpsertLearningPath(new LearningPath(
            pathId,
            learnerId,
            LearningPathStatus.Active,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));

        // Insert 5000 units
        for (int i = 0; i < 5000; i++)
        {
            repository.UpsertLearningPathUnit(new LearningPathUnit(
                $"unit-{i}",
                pathId,
                $"part{i % 7 + 1}-concept-{i}",
                i % 7 + 1,
                "[]",
                i + 1,
                i < 100 ? LearningPathUnitStatus.Unlocked : LearningPathUnitStatus.Locked,
                null,
                null
            ));
        }

        // Insert 1000 review items
        for (int i = 0; i < 1000; i++)
        {
            repository.UpsertReviewItem(new Domain.Aggregates.LearnerProgress.ReviewItem(
                $"review-{i}",
                learnerId,
                $"attempt-{i}",
                $"question-{i}",
                $"unit-{i % 100}",
                "tag",
                "A",
                "B",
                Domain.Aggregates.LearnerProgress.ReviewItemStatus.Open,
                false,
                DateTimeOffset.UtcNow.AddDays(-1),
                null
            ));
        }

        // Insert some assignments history
        for (int i = 0; i < 500; i++)
        {
            repository.UpsertLearnerAssignment(new LearnerAssignment(
                $"assign-{i}",
                learnerId,
                LearnerAssignmentType.Lesson,
                $"unit-{i % 100}",
                LearnerAssignmentStatus.Completed,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddHours(-1)
            ));
        }

        var handler = new GetLearnerTodayPlanHandler(repository);
        var query = new GetLearnerTodayPlanQuery(learnerId);

        // Warm up
        handler.Handle(query);

        int iterations = 10;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            handler.Handle(query);
        }

        sw.Stop();

        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        Console.WriteLine($"Average execution time over {iterations} runs: {avgMs:F2} ms");
        
        // Assert generous baseline
        Assert.True(avgMs < 200, $"Performance baseline exceeded. Avg: {avgMs}ms. Expected < 200ms");
    }

    public static void RepositoryEnforcesToeicDataIntegrityAndIndexes()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        SeedLearnerProfile(repository);

        Assert.Throws<SqliteException>(
            () => repository.UpsertLearnerAssignment(new LearnerAssignment(
                AssignmentId: "invalid-assignment-missing-learner",
                LearnerId: "missing-learner",
                AssignmentType: LearnerAssignmentType.MiniTest,
                ContentRefId: "test-full-toeic-001",
                Status: LearnerAssignmentStatus.Assigned,
                AssignedAtUtc: new DateTimeOffset(2026, 7, 2, 4, 0, 0, TimeSpan.Zero),
                DueAtUtc: null
            )),
            "Assignment must reference an existing learner profile."
        );
        Assert.Throws<SqliteException>(
            () => repository.UpsertLearnerAttempt(new LearnerAttempt(
                AttemptId: "invalid-attempt-missing-session",
                SessionId: "missing-session",
                LearnerId: "learner-production-001",
                Status: LearnerAttemptStatus.Submitted,
                CorrectCount: 1,
                TotalCount: 1,
                ScorePercent: 100,
                SubmittedAtUtc: new DateTimeOffset(2026, 7, 2, 4, 5, 0, TimeSpan.Zero)
            )),
            "Attempt must reference an existing activity session."
        );
        Assert.Throws<SqliteException>(
            () => repository.UpsertRepairAttempt(new RepairAttempt(
                RepairAttemptId: "invalid-repair-missing-review",
                ReviewItemId: "missing-review",
                LearnerId: "learner-production-001",
                Answer: "B",
                IsCorrect: true,
                AttemptedAtUtc: new DateTimeOffset(2026, 7, 2, 4, 10, 0, TimeSpan.Zero)
            )),
            "Repair attempt must reference an existing review item."
        );
    }
    public static void ScoresPlacementSessionAndPersistsResult()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        new OnboardLearnerHandler(repository).Handle(new OnboardLearnerCommand(
            "learner-score-001", "C", "c@example.com", 820, 510, 80, "Asia/Ho_Chi_Minh"
        ));
        
        var startResult = new StartPlacementSessionHandler(repository).Handle(new StartPlacementSessionCommand("learner-score-001"));
        
        // Mock assigned questions
        repository.UpsertPublishedLesson(new PublishedLesson("lesson1", "unit1", 5, "Title", "Obj", "[]", "[]", PublishedContentStatus.Published));
        
        var q1 = new PublishedQuestion("q1", "lesson1", 5, PublishedQuestionType.SingleQuestion, "Prompt 1", "{}", "A", "Expl", null, null, null, "[]", "[]", "[]", PublishedContentStatus.Published);
        var q2 = new PublishedQuestion("q2", "lesson1", 5, PublishedQuestionType.SingleQuestion, "Prompt 2", "{}", "B", "Expl", null, null, null, "[]", "[]", "[]", PublishedContentStatus.Published);
        repository.UpsertPublishedQuestion(q1);
        repository.UpsertPublishedQuestion(q2);
        
        repository.InsertPlacementSessionQuestions(startResult.SessionId, new[] { "q1", "q2" });

        var handler = new ScorePlacementSessionHandler(repository);
        var answers = new[]
        {
            new PlacementAnswerSubmission("q1", "A", Skipped: false),
            new PlacementAnswerSubmission("q2", null, Skipped: true)
        };

        var result = handler.Handle(new ScorePlacementSessionCommand(startResult.SessionId, answers));

        Assert.Equal(startResult.SessionId, result.SessionId, "Result must match session id.");
        Assert.Equal(1, result.CorrectCount, "One correct answer should yield 1 correct count.");
        Assert.Equal(2, result.TotalCount, "Two assigned questions yield 2 total count.");
        Assert.Equal(50, result.ScorePercent, "1/2 correct should be 50 percent.");
        Assert.True(result.DiagnosticScoreBand != null, "Diagnostic score band must be assigned.");
        Assert.Equal("GenerateLearningPath", result.NextAction.Code, "Next action must be path generation.");
        
        var persistedResult = repository.GetPlacementResultBySessionId(startResult.SessionId);
        Assert.True(persistedResult != null, "Placement result must be persisted.");
        Assert.Equal(50, persistedResult!.ScorePercent, "Persisted result must match calculated score.");
        
        var breakdowns = repository.GetPlacementResultBreakdowns(result.ResultId);
        Assert.True(breakdowns.Count > 0, "Part and skill breakdowns must be persisted.");
    }

    public static void AssignsLearnerTodayPlan()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        repository.UpsertLearnerProfile(new LearnerProfile(
            learnerId,
            "Test",
            "test@test.com",
            500,
            0,
            30,
            "UTC",
            LearnerProfileStatus.Active,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));

        // Create learning path
        var pathId = Guid.NewGuid().ToString();
        repository.UpsertLearningPath(new LearningPath(
            pathId,
            learnerId,
            LearningPathStatus.Active,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));

        repository.UpsertLearningPathUnit(new LearningPathUnit(
            "unit1",
            pathId,
            "part5-word-form",
            5,
            "[]",
            1,
            LearningPathUnitStatus.Unlocked,
            null,
            null
        ));

        var getHandler = new GetLearnerTodayPlanHandler(repository);
        var plan1 = getHandler.Handle(new GetLearnerTodayPlanQuery(learnerId));
        Assert.True(plan1.ReadyForNewAssignment, "Should be ready for new assignment");
        Assert.Null(plan1.PrimaryAssignment, "Should not generate assignment on GET");

        var handler = new GenerateNextAssignmentHandler(repository);
        var plan = handler.Handle(new GenerateNextAssignmentCommand(learnerId));

        Assert.NotNull(plan.PrimaryAssignment, "Should generate a primary assignment if none exists.");
    }

    public static void ManagesActivitySessions()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        var assignmentId = Guid.NewGuid().ToString();

        repository.UpsertLearnerProfile(new LearnerProfile(
            learnerId,
            "Test Session Learner",
            "test_session@test.com",
            500,
            0,
            30,
            "UTC",
            LearnerProfileStatus.Active,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));
        
        repository.UpsertLearnerAssignment(new LearnerAssignment(
            assignmentId,
            learnerId,
            LearnerAssignmentType.Lesson,
            "unit1",
            LearnerAssignmentStatus.Assigned,
            DateTimeOffset.UtcNow,
            null
        ));

        var handler = new ManageActivitySessionHandler(repository);
        
        // 1. Start creates session
        var startResult = handler.Handle(new StartActivitySessionCommand(assignmentId, learnerId));
        Assert.True(startResult.SessionId != null, "Start must return a session id.");
        Assert.Equal("InProgress", startResult.Status, "Session must be InProgress.");

        // 2. Duplicate start resumes (returns same session)
        var startResult2 = handler.Handle(new StartActivitySessionCommand(assignmentId, learnerId));
        Assert.Equal(startResult.SessionId, startResult2.SessionId, "Duplicate start must return same session.");

        // 3. Ownership is enforced
        Assert.Throws<ArgumentException>(() => 
            handler.Handle(new CompleteActivitySessionCommand(startResult.SessionId!, "wrong_learner")),
            "Ownership must be enforced."
        );

        // 4. Completion persists timestamp
        var completeResult = handler.Handle(new CompleteActivitySessionCommand(startResult.SessionId!, learnerId));
        Assert.Equal("Completed", completeResult.Status, "Completion must update status.");

        // 5. Invalid transition rejects
        Assert.Throws<ArgumentException>(() => 
            handler.Handle(new AbandonActivitySessionCommand(startResult.SessionId!, learnerId)),
            "Cannot abandon a completed session."
        );
    }

    public static void ProcessesLearnerAttempts()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        var assignmentId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();
        var questionId1 = Guid.NewGuid().ToString();
        var questionId2 = Guid.NewGuid().ToString();

        repository.UpsertLearnerProfile(new LearnerProfile(
            learnerId, "Test Learner", "test@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow
        ));

        repository.UpsertLearnerAssignment(new LearnerAssignment(
            assignmentId, learnerId, LearnerAssignmentType.Lesson, "unit1", LearnerAssignmentStatus.Started, DateTimeOffset.UtcNow, null
        ));

        repository.UpsertActivitySession(new ActivitySession(
            sessionId, assignmentId, learnerId, LearnerAssignmentType.Lesson, ActivitySessionStatus.InProgress, DateTimeOffset.UtcNow, null
        ));

        repository.UpsertPublishedLesson(new PublishedLesson(
            "lesson1", "unit1", 5, "Title", "Obj", "{}", "{}", PublishedContentStatus.Published
        ));

        repository.UpsertPublishedQuestion(new PublishedQuestion(
            questionId1, "lesson1", 5, PublishedQuestionType.SingleQuestion, "{}", "{}", "A", "{}", null, null, null, "{}", "{}", "{}", PublishedContentStatus.Published
        ));

        repository.UpsertPublishedQuestion(new PublishedQuestion(
            questionId2, "lesson1", 5, PublishedQuestionType.SingleQuestion, "{}", "{}", "C", "{}", null, null, null, "{}", "{}", "{}", PublishedContentStatus.Published
        ));

        var handler = new SubmitAttemptHandler(repository);

        var command = new SubmitAttemptCommand(sessionId, learnerId, new Dictionary<string, string>
        {
            { questionId1, "A" }, // Correct
            { questionId2, "A" }  // Incorrect
        });

        var response = handler.Handle(command);

        Assert.Equal(50, response.ScorePercent, "Score should be 50%.");
        Assert.Equal(1, response.CorrectCount, "One correct answer.");
        Assert.Equal(2, response.TotalCount, "Two total answers.");

        var session = repository.GetActivitySession(sessionId);
        Assert.True(session != null, "Session should exist");
        Assert.Equal(ActivitySessionStatus.Completed, session!.Status, "Session should be completed after attempt submission.");

        Assert.Throws<ArgumentException>(() => handler.Handle(command), "Should throw on duplicate submit");
    }

    public static void ReviewQueueCreatesAndUpdatesReviewItems()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        var assignmentId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();
        var questionId1 = Guid.NewGuid().ToString();

        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Test Learner", "test@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerAssignment(new LearnerAssignment(assignmentId, learnerId, LearnerAssignmentType.Lesson, "unit1", LearnerAssignmentStatus.Started, DateTimeOffset.UtcNow, null));
        repository.UpsertActivitySession(new ActivitySession(sessionId, assignmentId, learnerId, LearnerAssignmentType.Lesson, ActivitySessionStatus.InProgress, DateTimeOffset.UtcNow, null));
        repository.UpsertPublishedLesson(new PublishedLesson("lesson1", "unit1", 5, "Title", "Obj", "{}", "{}", PublishedContentStatus.Published));
        repository.UpsertPublishedQuestion(new PublishedQuestion(questionId1, "lesson1", 5, PublishedQuestionType.SingleQuestion, "{}", "{\"topic\":\"grammar\"}", "A", "{}", null, null, null, "{}", "{}", "{}", PublishedContentStatus.Published));

        var handler = new SubmitAttemptHandler(repository);

        // Attempt 1: wrong answer
        var command1 = new SubmitAttemptCommand(sessionId, learnerId, new Dictionary<string, string> { { questionId1, "B" } });
        var response1 = handler.Handle(command1);

        var reviewItems = repository.GetReviewItems(learnerId);
        Assert.Equal(1, reviewItems.Count, "Should create one review item");
        var item1 = reviewItems[0];
        Assert.Equal("B", item1.LearnerAnswer, "Should record learner answer");
        Assert.Equal(ReviewItemStatus.Open, item1.Status, "Status should be Open");
        Assert.False(item1.IsBlocking, "Lesson drill mistakes should not be blocking by default");

        // We need a new session for attempt 2, or reset session status
        var sessionId2 = Guid.NewGuid().ToString();
        repository.UpsertActivitySession(new ActivitySession(sessionId2, assignmentId, learnerId, LearnerAssignmentType.MiniTest, ActivitySessionStatus.InProgress, DateTimeOffset.UtcNow, null));

        // Attempt 2: wrong answer again, but from a MiniTest (which makes it blocking)
        var command2 = new SubmitAttemptCommand(sessionId2, learnerId, new Dictionary<string, string> { { questionId1, "C" } });
        handler.Handle(command2);

        reviewItems = repository.GetReviewItems(learnerId);
        Assert.Equal(1, reviewItems.Count, "Should not duplicate review item");
        var item2 = reviewItems[0];
        Assert.Equal("C", item2.LearnerAnswer, "Should update to latest wrong answer");
        Assert.Equal(ReviewItemStatus.Open, item2.Status, "Status should be Open");
        Assert.True(item2.IsBlocking, "MiniTest mistakes should be blocking");
    }

    public static void ReviewQueueResolvesReviewItems()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        var reviewItemId = Guid.NewGuid().ToString();

        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Test Learner", "test@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var reviewItem = new ReviewItem(
            reviewItemId, learnerId, "attempt1", "question1", "unit1", "{}", "B", "A", ReviewItemStatus.Open, true, DateTimeOffset.UtcNow, null
        );
        repository.UpsertReviewItem(reviewItem);

        var handler = new Application.Features.Learner.Review.ResolveReviewItemHandler(repository);

        // Wrong repair
        Assert.Throws<ArgumentException>(() => handler.Handle(new Application.Features.Learner.Review.ResolveReviewItemCommand(learnerId, reviewItemId, "C")), "Should throw if repair not passed");

        var item = repository.GetReviewItem(reviewItemId);
        Assert.Equal(ReviewItemStatus.Open, item!.Status, "Item should remain open");

        // Correct repair
        var response = handler.Handle(new Application.Features.Learner.Review.ResolveReviewItemCommand(learnerId, reviewItemId, "A"));
        Assert.True(response.IsCorrect, "Repair should be correct");

        item = repository.GetReviewItem(reviewItemId);
        Assert.Equal(ReviewItemStatus.Resolved, item!.Status, "Item should be resolved");
        Assert.True(item.ResolvedAtUtc != null, "ResolvedAtUtc should be set");
    }

    public static void ReviewQueueOrdersCorrectly()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Test Learner", "test@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var time1 = DateTimeOffset.UtcNow.AddMinutes(-10);
        var time2 = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Group 1, Unit A
        repository.UpsertReviewItem(new ReviewItem("item1", learnerId, "attempt", "q1", "unitA", "{}", "B", "A", ReviewItemStatus.Open, false, time1, null));
        repository.UpsertReviewItem(new ReviewItem("item2", learnerId, "attempt", "q2", "unitA", "{}", "C", "A", ReviewItemStatus.Open, true, time2, null));
        
        // Group 2, Unit B
        repository.UpsertReviewItem(new ReviewItem("item3", learnerId, "attempt", "q3", "unitB", "{}", "D", "A", ReviewItemStatus.Open, false, time1, null));

        var handler = new Application.Features.Learner.Review.GetLearnerReviewQueueHandler(repository);
        var response = handler.Handle(new Application.Features.Learner.Review.GetLearnerReviewQueueQuery(learnerId));

        Assert.Equal(2, response.Groups.Count, "Should have 2 groups");
        var unitAGroup = response.Groups.Single(g => g.UnitId == "unitA");
        
        Assert.Equal(2, unitAGroup.Items.Count, "Unit A should have 2 items");
        Assert.Equal("item2", unitAGroup.Items[0].ReviewItemId, "Blocking item should be first");
        Assert.Equal("item1", unitAGroup.Items[1].ReviewItemId, "Non-blocking item should be second");
    }

    public static void GeneratesLearnerPathFromPlacementResult()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = Guid.NewGuid().ToString();
        repository.UpsertLearnerProfile(new LearnerProfile(
            learnerId,
            "Test",
            "test@test.com",
            500,
            0,
            30,
            "UTC",
            LearnerProfileStatus.Active,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));

        var sessionId = Guid.NewGuid().ToString();
        var resultId = Guid.NewGuid().ToString();

        repository.UpsertPlacementSession(new PlacementSession(
            sessionId,
            learnerId,
            PlacementSessionStatus.Completed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));

        var placementResult = new PlacementResult(
            resultId,
            sessionId,
            learnerId,
            10,
            20,
            50,
            "Band 2",
            200,
            300,
            DateTimeOffset.UtcNow
        );

        var breakdown = new PlacementResultBreakdown(
            resultId,
            "Topic",
            "Word Form",
            1,
            5,
            20
        );

        repository.InsertPlacementResult(placementResult, [breakdown]);

        var handler = new GenerateLearningPathHandler(repository);
        var result = handler.Handle(new GenerateLearningPathCommand(learnerId, resultId));

        Assert.True(result.GeneratedReasonSummary.Contains(resultId), "Should include result id in reason.");
        Assert.True(result.PathId != null, "Path must be generated.");
        Assert.Equal("part5-word-form", result.FirstUnlockedUnitId, "Word form should be unlocked due to weakness.");
        Assert.True(result.TotalUnits > 0, "Catalog must be populated.");

        var activePath = repository.GetActiveLearningPath(learnerId);
        Assert.True(activePath != null, "Path must be persisted.");
        Assert.Equal(result.PathId, activePath!.PathId, "Persisted path must match result.");

        var units = repository.GetLearningPathUnits(activePath.PathId);
        Assert.Equal(result.TotalUnits, units.Count, "Persisted units must match total count.");
        
        var firstUnit = units.First(u => u.UnitId == result.FirstUnlockedUnitId);
        Assert.Equal(LearningPathUnitStatus.Unlocked, firstUnit.Status, "First unit must be unlocked.");
    }

    public static void RepositoryPersistsSourceAssetLinks()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var link = new SourceAssetLink("asset-key-1", "asset-book-1", SourceAssetRelationType.ProvidesAnswerKeyFor);
        repository.UpsertSourceAssetLink(link);
        
        var links = repository.GetSourceAssetLinks("asset-book-1");
        Assert.Equal(1, links.Count, "Should persist link");
        Assert.Equal("asset-key-1", links[0].SourceAssetId, "Should match source id");
    }

    public static void ToeicAnswerKeyParserExtractsKeys()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var asset = SeedSourceAsset(repository) with { DetectedRole = SourceAssetRole.AnswerKey };
        repository.UpsertSourceAsset(asset); // Update role if needed
        
        repository.UpsertExtractedPage(new ExtractedPage("page-1", asset.AssetId, 1, 500, 500, DateTimeOffset.UtcNow));
        
        // Simulate some blocks
        repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b1", asset.AssetId, "page-1", 1, ExtractedBlockType.Paragraph, "1. A  2. B", 1m, "{}"));
        repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b2", asset.AssetId, "page-1", 1, ExtractedBlockType.Paragraph, "3.C", 1m, "{}"));
        
        var parser = new Infrastructure.Extraction.ToeicAnswerKeyParser(repository, new[] { new Infrastructure.Extraction.Profiles.DefaultToeicParserProfile() });
        var results = parser.Parse(asset);
        
        Assert.True(results.Count == 3, $"Should extract 3 answers, got {results.Count}");
        Assert.Equal("A", results[0].CorrectAnswer, "Q1 should be A");
        Assert.Equal("B", results[1].CorrectAnswer, "Q2 should be B");
        Assert.Equal("C", results[2].CorrectAnswer, "Q3 should be C");
        Assert.True(results[0].Confidence < 0.9m, "Should flag low confidence because total isn't 100 or 200");
    }

    public static void ToeicAnswerKeyParserUsesProfiles()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var asset = SeedSourceAsset(repository) with { DetectedRole = SourceAssetRole.AnswerKey };
        repository.UpsertSourceAsset(asset);
        
        repository.UpsertExtractedPage(new ExtractedPage("page-1", asset.AssetId, 1, 500, 500, DateTimeOffset.UtcNow));
        repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b1", asset.AssetId, "page-1", 1, ExtractedBlockType.Paragraph, "GARBAGE", 1m, "{}"));
        
        var mockProfile = new MockHighConfidenceProfile();
        var parser = new Infrastructure.Extraction.ToeicAnswerKeyParser(repository, new[] { mockProfile });
        var results = parser.Parse(asset);
        
        Assert.True(results.Count == 1, "Should use the mock profile which returns 1 mapping");
        Assert.Equal("Z", results[0].CorrectAnswer, "Should use mock answer");
        Assert.Equal(0.99m, results[0].Confidence, "Should use mock confidence");
    }

    private class MockHighConfidenceProfile : Application.Features.SourceExtraction.IToeicParserProfile
    {
        public bool CanParse(SourceAsset asset) => true;
        public IReadOnlyList<Application.Features.SourceExtraction.AnswerKeyMappingResult> ParseAnswerKeys(IReadOnlyList<ExtractedTextBlock> blocks)
        {
            return new[] { new Application.Features.SourceExtraction.AnswerKeyMappingResult("mock-test", 1, "Z", 0.99m) };
        }
    }

    public static void ToeicTranscriptParserExtractsDialogs()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var asset = SeedSourceAsset(repository) with { DetectedRole = SourceAssetRole.Transcript };
        repository.UpsertSourceAsset(asset);
        
        repository.UpsertExtractedPage(new ExtractedPage("page-1", asset.AssetId, 1, 500, 500, DateTimeOffset.UtcNow));
        repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b1", asset.AssetId, "page-1", 1, ExtractedBlockType.Paragraph, "M: Hello there.\nW: Hi.", 1m, "{}"));
        
        var parser = new Infrastructure.Extraction.ToeicTranscriptParser(repository);
        var results = parser.Parse(asset);
        
        Assert.True(results.Count == 2, $"Should extract 2 segments, got {results.Count}");
        Assert.Equal("M", results[0].SpeakerLabel, "First speaker should be M");
        Assert.Equal("W", results[1].SpeakerLabel, "Second speaker should be W");
    }
    public static void LinkSourceAssetsHandlerPairsAssets()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        var book = SeedSourceAsset(repository);
        // Key in same container
        var key = book with { AssetId = "key-1", DetectedRole = SourceAssetRole.AnswerKey };
        // Transcript in same container
        var trans = book with { AssetId = "trans-1", DetectedRole = SourceAssetRole.Transcript };
        
        repository.UpsertSourceAsset(key);
        repository.UpsertSourceAsset(trans);
        
        var handler = new Application.Features.SourceClassification.LinkSourceAssetsHandler(repository);
        handler.Handle();
        
        var links = repository.GetSourceAssetLinks(book.AssetId);
        Assert.Equal(2, links.Count, "Should link both key and transcript");
    }

    public static void TodayPlanReadDoesNotMutateDb()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        var learnerId = "l1";
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile(learnerId, "abc", "abc@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new Domain.Aggregates.LearnerProgress.LearningPath("p1", learnerId, Domain.Aggregates.LearnerProgress.LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPathUnit(new Domain.Aggregates.LearnerProgress.LearningPathUnit("u1", "p1", "key1", 1, "tag", 1, Domain.Aggregates.LearnerProgress.LearningPathUnitStatus.Unlocked, null, null));
        
        // Test GET (should not create assignment)
        var getHandler = new Application.Features.Learner.Work.GetLearnerTodayPlanHandler(repository);
        var res1 = getHandler.Handle(new Application.Features.Learner.Work.GetLearnerTodayPlanQuery(learnerId));
        Assert.True(res1.ReadyForNewAssignment, "Should be ready");
        Assert.Null(res1.PrimaryAssignment, "Should not create assignment");
        
        // Test POST (should create assignment)
        var postHandler = new Application.Features.Learner.Work.GenerateNextAssignmentHandler(repository);
        var res2 = postHandler.Handle(new Application.Features.Learner.Work.GenerateNextAssignmentCommand(learnerId));
        Assert.NotNull(res2.PrimaryAssignment, "Should create assignment");
    }
}

sealed class FakeDriveDiscoveryGateway(IReadOnlyList<DriveDiscoveredAsset> assets) : IDriveDiscoveryGateway
{
    public IReadOnlyList<DriveDiscoveredAsset> ListFolderAssets(SourceManifestEntry source)
    {
        return assets;
    }
}

sealed class FakeExternalSourceResolver(IReadOnlyDictionary<string, ExternalSourceResolutionResult> results) : IExternalSourceResolver
{
    public ExternalSourceResolutionResult Resolve(string url)
    {
        return results[url];
    }
}

sealed class FakePdfTextBlockExtractor(IReadOnlyList<PdfExtractedPageResult> pages) : IPdfTextBlockExtractor
{
    public IReadOnlyList<PdfExtractedPageResult> Extract(SourceAsset asset)
    {
        return pages;
    }
}

sealed class FakeAudioMetadataProbe(AudioMetadataProbeResult result) : IAudioMetadataProbe
{
    public AudioMetadataProbeResult Probe(SourceAsset asset)
    {
        return result;
    }
}

sealed class FakeAnswerKeyParser(IReadOnlyList<AnswerKeyMappingResult> mappings) : IAnswerKeyParser
{
    public IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset)
    {
        return mappings;
    }
}

sealed class FakeTranscriptParser(IReadOnlyList<TranscriptSegmentResult> segments) : ITranscriptParser
{
    public IReadOnlyList<TranscriptSegmentResult> Parse(SourceAsset asset)
    {
        return segments;
    }
}

sealed class FakeReadingDraftParser(IReadOnlyList<ReadingDraftQuestionResult> questions) : IReadingDraftParser
{
    public IReadOnlyList<ReadingDraftQuestionResult> Parse(SourceAsset asset, IReadOnlyList<ExtractedTextBlock> blocks)
    {
        return questions;
    }
}


sealed class FakeListeningDraftParser(IReadOnlyList<ListeningDraftQuestionResult> questions) : IListeningDraftParser
{
    public IReadOnlyList<ListeningDraftQuestionResult> Parse(SourceAsset asset)
    {
        return questions;
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

    public static void Null(object? obj, string message)
    {
        if (obj is not null) throw new InvalidOperationException(message);
    }

    public static void NotNull(object? obj, string message)
    {
        if (obj is null) throw new InvalidOperationException(message);
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
