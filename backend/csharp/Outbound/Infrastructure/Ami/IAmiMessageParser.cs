namespace Outbound.Infrastructure.Ami;

public interface IAmiMessageParser
{
    AmiEventEnvelope Parse(string raw);
}
