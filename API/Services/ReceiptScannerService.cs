using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace API.Services
{
    public class ReceiptScannerService : IReceiptScannerService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReceiptScannerService> _logger;

        public ReceiptScannerService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ReceiptScannerService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var baseUrl = configuration["ReceiptScanner:BaseUrl"] ?? "http://localhost:5205";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
        }

        public async Task<ScannedReceiptResult> ScanReceiptAsync(
            byte[] imageData,
            string language = "fi",
            string provider = "auto",
            string? ollamaModel = null)
        {
            try
            {
                _logger.LogInformation("Scanning receipt with provider: {Provider}, language: {Language}", provider, language);

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageData);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(fileContent, "file", "receipt.jpg");

                var queryParams = $"?language={language}&provider={provider}";
                if (!string.IsNullOrEmpty(ollamaModel) && provider == "ollama")
                {
                    queryParams += $"&ollamaModel={ollamaModel}";
                }

                var response = await _httpClient.PostAsync(
                    $"/api/receipts/scan-file{queryParams}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Receipt scanner API returned {StatusCode}: {Error}",
                        response.StatusCode, errorContent);
                    throw new Exception($"Receipt scanning failed: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<ScannedReceiptResult>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                {
                    throw new Exception("Empty response from receipt scanner");
                }

                _logger.LogInformation("Receipt scanned successfully with {Provider}, found {LineCount} lines",
                    result.Provider, result.Lines.Count);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Receipt scanner API call failed - service may be unavailable");
                throw new Exception("Receipt scanning service unavailable. Please ensure the ReceiptScanner API is running.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Receipt scanner request timed out");
                throw new Exception("Receipt scanning timed out. The image may be too large or complex.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during receipt scanning");
                throw;
            }
        }
    }
}
