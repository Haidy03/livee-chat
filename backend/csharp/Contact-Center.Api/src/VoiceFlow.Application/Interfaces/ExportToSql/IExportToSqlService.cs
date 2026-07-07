using ExportToSql.Application.Contracts;

namespace ExportToSql.Application.Abstractions;

/// <summary>
/// Inbound port: the "export to SQL" use case. The API controller depends on
/// this interface, not on the concrete service.
/// </summary>
public interface IExportToSqlService
{
    Task<ExportToSqlResponse> ExportAsync(
        ExportToSqlRequest request, CancellationToken cancellationToken = default);
}
