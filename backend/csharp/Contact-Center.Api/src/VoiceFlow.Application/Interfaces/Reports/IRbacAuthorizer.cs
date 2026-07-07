namespace VoiceFlow.Application.Interfaces.Reports;

public enum ReportsAction { View, Create, Edit, Delete, Export }

public interface IRbacAuthorizer
{
    Task<bool> CanAsync(string tenantId, string userId, ReportsAction action, CancellationToken ct);
}
