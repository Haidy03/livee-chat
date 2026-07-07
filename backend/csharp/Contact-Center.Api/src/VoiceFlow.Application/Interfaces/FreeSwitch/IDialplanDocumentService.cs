using VoiceFlow.Contracts.FreeSwitch;
using VoiceFlow.Core.Common;
using VoiceFlow.Domain.FreeSwitch;
namespace VoiceFlow.Application.Interfaces.FreeSwitch;

public interface IDialplanDocumentService
{
    Task<Result<PushDialplanDocumentsResponse>> PushAsync(PushDialplanDocumentsRequest request, CancellationToken ct);
}
