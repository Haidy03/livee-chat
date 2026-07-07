using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>
/// A single long-lived headless Chromium, shared across all PDF renders. "Lite" comes
/// from reusing one browser process (never launch per report) and a concurrency gate,
/// not from a weaker engine. Each render gets a fresh page that is closed afterwards.
/// Registered as a singleton; disposed on host shutdown.
/// </summary>
public sealed class ChromiumPageRenderer : IAsyncDisposable
{
    private readonly ILogger<ChromiumPageRenderer> _log;
    private readonly SemaphoreSlim _gate;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IBrowser? _browser;

    public ChromiumPageRenderer(ILogger<ChromiumPageRenderer> log)
    {
        _log = log;
        // Cap concurrent pages so a burst of scheduled reports can't exhaust memory.
        _gate = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2));
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser is { IsConnected: true }) return _browser;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_browser is { IsConnected: true }) return _browser;

            var options = new LaunchOptions
            {
                Headless = true,
                // Flags that let Chromium run in restricted/rootless/container environments
                // (no user namespaces, no /dev/shm, no GPU, no zygote OOM tuning).
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--no-zygote",
                },
            };

            // Prefer a Chromium installed in the image / on the host (pinned via env, or
            // auto-detected). We only download PuppeteerSharp's own copy as a last resort —
            // in containers the FS is ephemeral (every pod would re-fetch) and the download
            // may miss shared libs the OS-packaged Chromium already has.
            var executablePath = ResolveExecutablePath();
            if (executablePath is not null)
            {
                options.ExecutablePath = executablePath;
                _log.LogInformation("Launching Chromium from {Path} for report rendering.", executablePath);
            }
            else
            {
                _log.LogInformation("No system Chromium found; fetching one for report rendering (first use only)…");
                await new BrowserFetcher().DownloadAsync();
            }

            _browser = await Puppeteer.LaunchAsync(options);
            _log.LogInformation("Chromium launched for report rendering.");
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>Render a full HTML document to a PDF, waiting for the chart bundle to paint.</summary>
    public async Task<byte[]> RenderPdfAsync(string html, CancellationToken ct)
    {
        var browser = await GetBrowserAsync(ct);

        await _gate.WaitAsync(ct);
        IPage? page = null;
        try
        {
            page = await browser.NewPageAsync();
            // A4 at 96dpi so the chart lays out at the printed width.
            await page.SetViewportAsync(new ViewPortOptions { Width = 794, Height = 1123, DeviceScaleFactor = 2 });
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
            });
            // The bundle sets window.__chartReady once Recharts has painted (and hard-stops
            // at 4s itself), so this wait resolves quickly and never hangs the render.
            await page.WaitForFunctionAsync("() => window.__chartReady === true",
                new WaitForFunctionOptions { Timeout = 8000 });

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
            });
        }
        finally
        {
            if (page is not null) await page.CloseAsync();
            _gate.Release();
        }
    }

    /// <summary>Env override first, then common OS-packaged Chrome/Chromium locations; null → download.</summary>
    private static string? ResolveExecutablePath()
    {
        var fromEnv = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv)) return fromEnv;

        string[] candidates =
        {
            "/usr/bin/google-chrome",
            "/usr/bin/google-chrome-stable",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _gate.Dispose();
        _initLock.Dispose();
    }
}
