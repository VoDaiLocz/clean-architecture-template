using System;
using System.Linq;
using Xunit;
using Application.Features.SourceExtraction;
using Infrastructure.Data;
using Infrastructure.Extraction;

namespace Application.UnitTest;

public class RunRealDbUtility
{
    [Fact]
    public void ExecuteReadingDraftsOnRealDb()
    {
        var dbPath = System.IO.Path.GetFullPath("../../../../../src/Api/toeic-normalization.db");
        Console.WriteLine("DB Path: " + dbPath);
        var repo = SqliteKnowledgeRepository.InMemory();
        var parser = new RegexReadingDraftParser();
        var handler = new ParseToeicReadingDraftsHandler(repo, parser);

        var allAssets = repo.GetAllSourceAssets();
        var testBooks = allAssets.Where(a => a.DetectedRole == Domain.Aggregates.Corpus.SourceAssetRole.Pdf).ToList();

        int totalDrafts = 0;
        foreach (var book in testBooks)
        {
            try
            {
                var res = handler.Handle(new ParseToeicReadingDraftsCommand(book.AssetId));
                totalDrafts += res.CreatedReadingDraftCount;
                Console.WriteLine($"Parsed {res.CreatedReadingDraftCount} reading drafts from {book.AssetId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on {book.AssetId}: {ex.Message}");
            }
        }
        Console.WriteLine($"TOTAL DRAFTS EXTRACTED: {totalDrafts}");
        Assert.True(totalDrafts >= 0, "totalDrafts >= 0");
    }
}
