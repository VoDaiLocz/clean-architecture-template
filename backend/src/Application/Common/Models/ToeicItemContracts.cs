using System.Collections.Generic;

namespace Application.Common.Models;

public interface IPartPayload { }

// Part-specific payloads
public class Part1Payload : IPartPayload { }
public class Part2Payload : IPartPayload { }
public class Part3Payload : IPartPayload { }
public class Part4Payload : IPartPayload { }
public class Part5Payload : IPartPayload { }
public class Part6Payload : IPartPayload { }
public class Part7Payload : IPartPayload { }

// Shared abstract base class to enforce consistency
public abstract class ToeicItemBase
{
    public required string Id { get; set; }
    public required int Part { get; set; }
    
    public string? Prompt { get; set; }
    public string[]? Choices { get; set; }
    
    // References
    public List<string>? MediaRefs { get; set; }
    public string? GroupRef { get; set; }
    public List<string>? PassageRefs { get; set; }

    // Part-specific payload
    public IPartPayload? Payload { get; set; }
}

/// <summary>
/// Learner-safe contract for test taking.
/// Does NOT contain CorrectAnswer or Explanation.
/// </summary>
public class ToeicPlayableItem : ToeicItemBase
{
}

/// <summary>
/// Contract for test review (after submission).
/// Contains CorrectAnswer and Explanation.
/// </summary>
public class ToeicReviewItem : ToeicItemBase
{
    public required string CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
}

/// <summary>
/// Contract for result summaries (e.g. score, correctness per item).
/// </summary>
public class ToeicResultItem
{
    public required string ItemId { get; set; }
    public required int Part { get; set; }
    public required bool IsCorrect { get; set; }
    public required string SelectedAnswer { get; set; }
    public required string CorrectAnswer { get; set; }
    public int Score { get; set; }
}

/// <summary>
/// Admin contract containing everything including SkillTags and internal metadata.
/// </summary>
public class AdminPublishedItem : ToeicItemBase
{
    public required string CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public List<string>? SkillTags { get; set; }
    public string? InternalNotes { get; set; }
}
