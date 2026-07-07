using System.Collections.Concurrent;
using CTI.Models.HubSpot;
using CtiBackend.Models.Cti;
using CtiBackend.Options;
using CtiBackend.Services.Directory;
using CtiBackend.Services.HubSpot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.CallerInfo;

/// <summary>
/// Mock in-memory caller-info resolver. Replace with a CRM HTTP integration
/// using the injected <see cref="IHttpClientFactory"/>; see the TODO block.
/// </summary>
public sealed class CallerInfoResolver : ICallerInfoResolver
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly CallerInfoOptions _options;
    private readonly IServiceScopeFactory _scopes;
    private readonly IContactDirectoryService _directory;
    private readonly ILogger<CallerInfoResolver> _log;

    private static readonly ConcurrentDictionary<string, CallerInfoModel> Mock = new(new[]
    {
        new KeyValuePair<string, CallerInfoModel>("966500000001", new CallerInfoModel
        {
            PhoneNumber = "966500000001", CustomerId = "C-1001", Name = "Mohammed Ali",
            Type = "VIP", IsVip = true, Segment = "Premium",
            NationalId = "1099887766", AccountNumber = "ACC-7788",
        }),
        new KeyValuePair<string, CallerInfoModel>("966500000002", new CallerInfoModel
        {
            PhoneNumber = "966500000002", CustomerId = "C-1002", Name = "Sara Khaled",
            Type = "Retail", IsVip = false, Segment = "Standard",
            NationalId = "1023456789", AccountNumber = "ACC-2233",
        }),
    });

    public CallerInfoResolver(
        IHttpClientFactory httpFactory,
        IOptions<CallerInfoOptions> options,
        IServiceScopeFactory scopes,
         IContactDirectoryService directory,
        ILogger<CallerInfoResolver> log)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _scopes = scopes;
        _log = log;
        _directory = directory;
    }

    public async Task<CallerInfoModel?> ResolveAsync(string? tenantId, string? phoneNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        // Opportunistic HubSpot enrichment.
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var lookup = scope.ServiceProvider.GetRequiredService<IHubSpotCallerLookupService>();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));
                var result = await lookup.FindCallerAsync(tenantId!, phoneNumber!, cts.Token);
                if (result.Found && result.PrimaryContact is { } c)
                {
                    return new CallerInfoModel
                    {
                        PhoneNumber = phoneNumber,
                        CustomerId = c.HubSpotContactId,
                        Name = c.FullName ?? string.Join(' ', new[] { c.FirstName, c.LastName }
                            .Where(s => !string.IsNullOrWhiteSpace(s))!),
                        Type = c.LifecycleStage,
                        Segment = c.Company,
                    };
                }
            }
            catch (HubSpotLookupException ex)
            {
                _log.LogDebug("HubSpot caller lookup skipped ({Code})", ex.Code);
            }
            catch (OperationCanceledException)
            {
                _log.LogDebug("HubSpot caller lookup timed out");
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "HubSpot caller lookup failed; falling back to mock");
            }
        }

        // Fallback mock
        await Task.Yield();
        if (Mock.TryGetValue(phoneNumber, out var info)) return info;
        return new CallerInfoModel { PhoneNumber = phoneNumber, Name = "Unknown", IsVip = false };
    }

    public async Task<CallerInfoModel?> ResolveFromDirectoryAsync(string? tenantId, string? phoneNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        try
        {
            var contact = await _directory.FindByPhoneAsync(tenantId, phoneNumber!, ct);
            if (contact != null)
            {
                return new CallerInfoModel
                {
                    PhoneNumber = phoneNumber,
                    CustomerId = contact.Id,
                    Name = contact.Name,
                    Segment = contact.Company,
                    Type = contact.TagIds is { Count: > 0 } tags ? tags[0] : null,
                    Extra =
                    {
                        ["email"] = contact.Email ?? string.Empty,
                        ["company"] = contact.Company ?? string.Empty,
                        ["totalCalls"] = contact.TotalCalls.ToString(),
                    },
                };
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Contact directory lookup failed");
        }

        return null;
    }


}
