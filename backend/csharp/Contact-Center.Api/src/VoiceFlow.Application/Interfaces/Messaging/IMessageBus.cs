namespace VoiceFlow.Application.Interfaces.Messaging
{
    public interface IMessageBus
    {
        Task PublishAsync<T>(T messageModel, string routingKey, CancellationToken cancellationToken = default);
    }
}
