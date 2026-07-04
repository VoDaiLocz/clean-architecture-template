using Application.Common.Models;
using System.Text.Json;

public static class ToeicItemContractsTests
{
    public static void ToeicPlayableItemDoesNotLeakAnswer()
    {
        var playable = new ToeicPlayableItem
        {
            Id = "item-1",
            Part = 5,
            Prompt = "The manager ___ the meeting.",
            Choices = new[] { "A. canceled", "B. cancel", "C. canceling", "D. cancellation" },
            Payload = new Part5Payload()
        };

        var json = JsonSerializer.Serialize(playable);
        
        if (json.Contains("CorrectAnswer", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("Explanation", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Playable item leaked answer keys: {json}");
        }
    }

    public static void ToeicReviewItemContainsAnswer()
    {
        var review = new ToeicReviewItem
        {
            Id = "item-1",
            Part = 5,
            Prompt = "The manager ___ the meeting.",
            Choices = new[] { "A. canceled", "B. cancel", "C. canceling", "D. cancellation" },
            CorrectAnswer = "A",
            Explanation = "Past tense is required.",
            Payload = new Part5Payload()
        };

        var json = JsonSerializer.Serialize(review);
        
        if (!json.Contains("CorrectAnswer", StringComparison.OrdinalIgnoreCase) ||
            !json.Contains("Explanation", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Review item missing answer keys: {json}");
        }
    }
}
