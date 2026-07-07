namespace CtiBackend.Models.HubSpot;

public enum HubSpotIntegrationStatus
{
    Disconnected = 0,
    Connected = 1,
    Expired = 2,
    Revoked = 3,
    Error = 4,
}
