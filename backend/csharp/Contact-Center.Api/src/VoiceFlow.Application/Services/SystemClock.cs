
using VoiceFlow.Application.Common;

namespace VoiceFlow.Application.Services
{
    public class SystemClock: IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
