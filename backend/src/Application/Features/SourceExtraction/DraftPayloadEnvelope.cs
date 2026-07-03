using System.Text.Json;

namespace Application.Features.SourceExtraction;

internal static class DraftPayloadEnvelope
{
    public const string CurrentSchemaVersion = "toeic-draft.v1";

    public static string Serialize(string kind, object data)
    {
        return JsonSerializer.Serialize(new
        {
            schemaVersion = CurrentSchemaVersion,
            kind,
            data,
        });
    }
}
