namespace VoiceFlow.Application.Interfaces.Messaging;

/// <summary>
/// Publisher for call-related integration events. Uses topology from configuration section <c>CallPublisher</c>.
/// </summary>
public interface ICallPublisher : IMessageBus
{
}
