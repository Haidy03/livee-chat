using Contact_Center.Worker.Events.CallEvent.Constants;
using Contact_Center.Worker.Events.CallEvent.Services;
using VoiceFlow.Contracts.Events;

namespace Contact_Center.Worker.Events.CallEvent.Handlers;

public sealed class CallTerminatedEventHandler
{
    private readonly CallTermenatedProcessingService _service;

    public CallTerminatedEventHandler(
        CallTermenatedProcessingService service)
    {
        _service = service;
    }

    public async Task HandleAsync(
        CallTerminatedEvent message,
        CancellationToken cancellationToken)
    {

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        switch (message.Event)
        {
            case VoiceEvents.CallTerminated:
                await _service.ProcessCallTerminatedAsync(
                    message,
                    cancellationToken);
                break;
            case VoiceEvents.VoicePublished:
                await _service.ProcessVoicePublishedAsync(
                    message,
                    cancellationToken);
                break;
            case VoiceEvents.VoicemailRecorded:
                await _service.ProcessVoicemailRecordedAsync(
                    message,
                    cancellationToken);
                break;
            default:
                return;
        }


    }
}