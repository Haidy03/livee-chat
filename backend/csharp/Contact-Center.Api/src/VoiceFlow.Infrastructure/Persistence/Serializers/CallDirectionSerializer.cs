using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Infrastructure.Persistence.Serializers;

/// <summary>
/// Persists <see cref="CallDirection"/> as the canonical lowercase strings
/// "inbound" / "outbound" / "internal" used across the system.
/// Reads:
///   - lowercase canonical strings ("inbound", "outbound", "internal")
///   - legacy enum names ("Incoming", "Outgoing", "Internal", case-insensitive)
///   - legacy integer values (0 = Inbound, 1 = Outbound, 2 = Internal)
/// </summary>
public sealed class CallDirectionSerializer : IBsonSerializer<CallDirection>, IBsonSerializer
{
    public Type ValueType => typeof(CallDirection);

    public CallDirection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var type = reader.GetCurrentBsonType();

        switch (type)
        {
            case BsonType.String:
                return ParseString(reader.ReadString());
            case BsonType.Int32:
                return FromInt(reader.ReadInt32());
            case BsonType.Int64:
                return FromInt((int)reader.ReadInt64());
            case BsonType.Null:
                reader.ReadNull();
                return CallDirection.Inbound;
            default:
                reader.SkipValue();
                return CallDirection.Inbound;
        }
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CallDirection value)
    {
        context.Writer.WriteString(ToCanonical(value));
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => Deserialize(context, args);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        => Serialize(context, args, (CallDirection)value);

    public static string ToCanonical(CallDirection value) => value switch
    {
        CallDirection.Inbound => "inbound",
        CallDirection.Outbound => "outbound",
        CallDirection.Internal => "internal",
        _ => "inbound"
    };

    private static CallDirection ParseString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return CallDirection.Inbound;

        return value.Trim().ToLowerInvariant() switch
        {
            "inbound" or "incoming" or "in" => CallDirection.Inbound,
            "outbound" or "outgoing" or "out" => CallDirection.Outbound,
            "internal" or "self" => CallDirection.Internal,
            _ => CallDirection.Inbound
        };
    }

    private static CallDirection FromInt(int value) => value switch
    {
        0 => CallDirection.Inbound,
        1 => CallDirection.Outbound,
        2 => CallDirection.Internal,
        _ => CallDirection.Inbound
    };
}
