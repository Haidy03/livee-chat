using VoiceFlow.Core.Enums.Survey;

namespace VoiceFlow.Core.Exceptions.Surveys;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class SurveyNotFoundException : DomainException
{
    public SurveyNotFoundException(string id) : base($"Survey '{id}' was not found.") { }
}

public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(SurveyStatus from, SurveyStatus to)
        : base($"Cannot transition survey from {from} to {to}.") { }
}

public sealed class BranchTargetMissingException : DomainException
{
    public BranchTargetMissingException(string questionId)
        : base($"Branching rule targets unknown question '{questionId}'.") { }
}

public sealed class InvalidWebhookSignatureException : DomainException
{
    public InvalidWebhookSignatureException() : base("Invalid webhook signature.") { }
}
