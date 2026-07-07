using CtiBackend.Models.Ami;
using CtiBackend.Models.Cti;

namespace CtiBackend.Services.State;

public interface ICallSessionStateManager
{
    CallSessionState ApplyEvent(AmiEventEnvelope env);

    IReadOnlyList<CallSessionState> Active();
    IReadOnlyList<CallSessionState> RecentEnded();
    CallSessionState? GetById(string sessionId);
    CallSessionState? GetByLinkedId(string linkedId);
    CallSessionState? GetByUniqueId(string uniqueId);
    IReadOnlyList<CallSessionState> GetByCaller(string callerNumber);

    void UpdateCallerInfo(string sessionId, CallerInfoModel info);
}
