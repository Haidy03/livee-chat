using CtiBackend.Models.Ami;

namespace CtiBackend.Services.Ami;

public interface IAmiMessageParser
{
    AmiEventEnvelope Parse(string raw);
}
