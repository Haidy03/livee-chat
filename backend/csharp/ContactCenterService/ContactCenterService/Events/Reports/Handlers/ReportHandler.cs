using Contact_Center.Worker.Events.Reports.constatnts;
using Contact_Center.Worker.Events.Reports.Services;
using VoiceFlow.Contracts.Events;

namespace Contact_Center.Worker.Events.Reports.Handlers
{
    public class ReportHandler
    {
        private readonly ReportRunner _service;

        public ReportHandler(
            ReportRunner service)
        {
            _service = service;
        }

        public async Task HandleAsync(
            ReportRunRequested message,
            CancellationToken cancellationToken)
        {

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            switch (message.Event)
            {
                case ReportEvents.ReportRunRequested:
                    await _service.RunAsync(
                        message,
                        cancellationToken);
                    break;
                default:
                    return;
            }


        }
    }
}
