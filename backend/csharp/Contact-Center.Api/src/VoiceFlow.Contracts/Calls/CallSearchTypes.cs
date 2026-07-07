namespace VoiceFlow.Contracts.Calls;

public enum CallTypeFilter
{
    Inbound,
    Outbound,
    Internal,
    Self
}

public enum CallPropertyFilter
{
    Recording,
    Voicemail,
    Transfer,
    Hold
}

public enum HandledByFilter
{
    Any,
    Agent,
    Ai,
    Ivr
}

public enum SearchOperatorFilter
{
    And,
    Or,
    Phrase
}

public enum SortDirectionFilter
{
    Asc,
    Desc
}
