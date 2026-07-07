namespace VoiceFlow.Api.LiveChat.Config;

/// <summary>
/// Runtime marker recording which concrete store implementation was
/// registered for LiveChat presence / offer-timeout state so operators
/// can tell at a glance whether Redis is actually being written to.
/// </summary>
public sealed record LiveChatStoreInfo(string PresenceStoreType, string OfferTimeoutStoreType);
