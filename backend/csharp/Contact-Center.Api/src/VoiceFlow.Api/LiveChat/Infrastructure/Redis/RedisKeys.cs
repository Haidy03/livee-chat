namespace VoiceFlow.Api.LiveChat.Infrastructure.Redis;

internal static class RedisKeys
{
    public const string Prefix = "livechat:";
    public static string Presence(string agentId) => $"{Prefix}presence:{agentId}";
    public static string Conns(string agentId) => $"{Prefix}conns:{agentId}";
    public static string Dept(string departmentId) => $"{Prefix}dept:{departmentId}";
    public const string Offers = Prefix + "offers";

    // Presence hash fields
    public const string HStatus = "status";
    public const string HActive = "activeChats";
    public const string HMax = "maxConcurrency";
    public const string HLast = "lastAssignedAt";
    public const string HDepts = "departmentIds";
    public const string HLangs = "languages";
}
