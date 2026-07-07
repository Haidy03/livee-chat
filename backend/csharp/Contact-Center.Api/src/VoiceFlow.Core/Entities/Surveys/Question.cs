using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using VoiceFlow.Core.Enums.Survey;

namespace VoiceFlow.Core.Entities.Surveys;

public class Question
{
    public string Id { get; set; } = "";
    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public QuestionType Type { get; set; }

    [BsonElement("text")]
    public string Text { get; set; } = "";

    [BsonElement("required")]
    public bool Required { get; set; }

    [BsonElement("config")]
    public QuestionConfig Config { get; set; } = default!;

    [BsonElement("soundFile")]
    public SoundFileInfo SoundFile { get; set; } = new();

    [BsonElement("branchingRules")]
    public List<BranchingRule> BranchingRules { get; set; } = new();
}


public class SoundFileInfo
{
    [JsonPropertyName("source")]
    public SoundFileSource Source { get; set; }

    [JsonPropertyName("voiceLibraryId")]
    public string? VoiceLibraryId { get; set; }   // when Source = Library

    [JsonPropertyName("url")]
    public string? Url { get; set; }              // resolved playback URL

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }         // for upload display
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SoundFileSource
{
    Library,
    Upload
}

public class BranchingRule
{
    [BsonElement("id")]
    public string Id { get; set; } = "";

    [BsonElement("condition")]
    public string Condition { get; set; } = "";

    [BsonElement("action")]
    public BranchAction Action { get; set; } = default!;
}

[BsonDiscriminator("branchAction")]
[BsonKnownTypes(
    typeof(GotoBranchAction),
    typeof(EndBranchAction))]
public abstract class BranchAction
{
    [BsonElement("kind")]
    [BsonRepresentation(BsonType.String)]
    public abstract BranchActionKind Kind { get; }
}

[BsonDiscriminator("goto")]
public sealed class GotoBranchAction : BranchAction
{
    public override BranchActionKind Kind => BranchActionKind.Goto;
    
    [BsonElement("questionId")]
    public string QuestionId { get; set; } = "";
}

[BsonDiscriminator("end")]
public sealed class EndBranchAction : BranchAction
{
    public override BranchActionKind Kind => BranchActionKind.End;
}
