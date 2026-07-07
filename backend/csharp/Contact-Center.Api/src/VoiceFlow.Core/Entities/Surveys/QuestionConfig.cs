using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using VoiceFlow.Core.Enums.Survey;

namespace VoiceFlow.Core.Entities.Surveys;


[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RatingConfig), "rating")]
[JsonDerivedType(typeof(YesNoConfig), "yesNo")]
[JsonDerivedType(typeof(MultipleChoiceConfig), "multipleChoice")]
[JsonDerivedType(typeof(NumericConfig), "numeric")]
[JsonDerivedType(typeof(VoiceConfig), "voice")]
[JsonDerivedType(typeof(NpsConfig), "nps")]

[BsonDiscriminator("questionConfig")]
[BsonKnownTypes(
    typeof(RatingConfig),
    typeof(YesNoConfig),
    typeof(MultipleChoiceConfig),
    typeof(NumericConfig),
    typeof(VoiceConfig),
    typeof(NpsConfig))]
public abstract class QuestionConfig
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public abstract QuestionType Type { get; }
}

[BsonDiscriminator("rating")]
public sealed class RatingConfig : QuestionConfig
{
    public override QuestionType Type => QuestionType.Rating;
    [BsonElement("min")]
    public int Min { get; set; }

    [BsonElement("max")]
    public int Max { get; set; }

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }
}

[BsonDiscriminator("yesNo")]
public sealed class YesNoConfig : QuestionConfig
{
    public override QuestionType Type => QuestionType.YesNo;
    [BsonElement("yesKey")]
    public string YesKey { get; set; } = "1";

    [BsonElement("noKey")]
    public string NoKey { get; set; } = "2";
}

public sealed class MultipleChoiceChoice
{
    [BsonElement("key")]
    public string Key { get; set; } = "";

    [BsonElement("label")]
    public string Label { get; set; } = "";
}

[BsonDiscriminator("multipleChoice")]
public sealed class MultipleChoiceConfig : QuestionConfig
{
    public override QuestionType Type => QuestionType.MultipleChoice;
    [BsonElement("choices")]
    public List<MultipleChoiceChoice> Choices { get; set; } = new();
}

[BsonDiscriminator("numeric")]
public sealed class NumericConfig : QuestionConfig
{
    public override QuestionType Type => QuestionType.Numeric;
    [BsonElement("minDigits")]
    public int MinDigits { get; set; }

    [BsonElement("maxDigits")]
    public int MaxDigits { get; set; }

    [BsonElement("terminator")]
    public string Terminator { get; set; } = "#";
}


[BsonDiscriminator("voice")]
public sealed class VoiceConfig : QuestionConfig
{
    public override QuestionType Type => QuestionType.Voice;
    [BsonElement("maxDurationSec")]
    public int MaxDurationSec { get; set; }

    [BsonElement("beepBefore")]
    public bool BeepBefore { get; set; }
}

[BsonDiscriminator("nps")]
public sealed class NpsConfig : QuestionConfig
{
    public override QuestionType Type => QuestionType.Nps;
    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }
}
