using System;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace API.Services
{
    public class PdfService : IPdfService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<PdfService> _logger;
        private readonly string _appUrl;
        private IBrowser _browser;
        private readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);

        public PdfService(IConfiguration config, ILogger<PdfService> logger)
        {
            _config = config;
            _logger = logger;
            _appUrl = _config["Email:AppUrl"] ?? "https://localhost:3000";
        }

        private async Task<IBrowser> GetBrowserAsync()
        {
            await _browserLock.WaitAsync();
            try
            {
                if (_browser == null || !_browser.IsConnected)
                {
                    _logger.LogInformation("Initializing Puppeteer browser...");

                    // Download Chromium if not already downloaded
                    var browserFetcher = new BrowserFetcher();
                    await browserFetcher.DownloadAsync();

                    _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true,
                        Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                    });

                    _logger.LogInformation("Puppeteer browser initialized successfully");
                }

                return _browser;
            }
            finally
            {
                _browserLock.Release();
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
        {
            try
            {
                var browser = await GetBrowserAsync();
                var page = await browser.NewPageAsync();

                try
                {
                    var url = $"{_appUrl}/invoices/{invoiceId}/print";
                    _logger.LogInformation("Generating full invoice PDF from URL: {Url}", url);

                    await page.GoToAsync(url, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                        Timeout = 30000
                    });

                    // Wait a bit for any dynamic content to render
                    await Task.Delay(1000);

                    var pdfData = await page.PdfDataAsync(new PdfOptions
                    {
                        Format = PaperFormat.A4,
                        PrintBackground = true,
                        MarginOptions = new MarginOptions
                        {
                            Top = "20px",
                            Right = "20px",
                            Bottom = "20px",
                            Left = "20px"
                        }
                    });

                    _logger.LogInformation("Successfully generated full invoice PDF for invoice {InvoiceId}", invoiceId);
                    return pdfData;
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating full invoice PDF for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<byte[]> GenerateParticipantInvoicePdfAsync(Guid invoiceId, string participantId)
        {
            try
            {
                var browser = await GetBrowserAsync();
                var page = await browser.NewPageAsync();

                try
                {
                    var url = $"{_appUrl}/invoices/{invoiceId}/print/{participantId}";
                    _logger.LogInformation("Generating participant invoice PDF from URL: {Url}", url);

                    await page.GoToAsync(url, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                        Timeout = 30000
                    });

                    // Wait a bit for any dynamic content to render
                    await Task.Delay(1000);

                    var pdfData = await page.PdfDataAsync(new PdfOptions
                    {
                        Format = PaperFormat.A4,
                        PrintBackground = true,
                        MarginOptions = new MarginOptions
                        {
                            Top = "20px",
                            Right = "20px",
                            Bottom = "20px",
                            Left = "20px"
                        }
                    });

                    _logger.LogInformation("Successfully generated participant invoice PDF for invoice {InvoiceId}, participant {ParticipantId}", invoiceId, participantId);
                    return pdfData;
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating participant invoice PDF for invoice {InvoiceId}, participant {ParticipantId}", invoiceId, participantId);
                throw;
            }
        }
    }
}
