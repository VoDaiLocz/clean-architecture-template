using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Application.Common.Interfaces.Repositories;
using Application.Features.SourceExtraction;
using Infrastructure.Data;
using Infrastructure.Extraction;

var services = new ServiceCollection();
services.AddSingleton<IKnowledgeRepository>(sp => new SqliteKnowledgeRepository("Data Source=toeic-normalization.db"));
services.AddTransient<IReadingDraftParser, RegexReadingDraftParser>();
services.AddTransient<ParseToeicReadingDraftsHandler>();

var provider = services.BuildServiceProvider();
var repo = provider.GetRequiredService<IKnowledgeRepository>();
var handler = provider.GetRequiredService<ParseToeicReadingDraftsHandler>();

var allAssets = repo.GetAllSourceAssets();
var testBooks = allAssets.Where(a => a.DetectedRole == Domain.Aggregates.Corpus.SourceAssetRole.TestBook || a.DetectedRole == Domain.Aggregates.Corpus.SourceAssetRole.Pdf).ToList();

Console.WriteLine($"Found {testBooks.Count} PDFs/TestBooks.");

int totalDrafts = 0;
foreach (var book in testBooks)
{
    try 
    {
        var res = handler.Handle(new ParseToeicReadingDraftsCommand(book.AssetId));
        totalDrafts += res.CreatedReadingDraftCount;
        Console.WriteLine($"Parsed {res.CreatedReadingDraftCount} reading drafts from {book.Title}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error on {book.Title}: {ex.Message}");
    }
}

Console.WriteLine($"Total Reading Drafts Created: {totalDrafts}");
