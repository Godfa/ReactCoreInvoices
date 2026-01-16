using System;
using System.IO;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly IReceiptScannerService _scannerService;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(
            IReceiptScannerService scannerService,
            ILogger<ReceiptsController> logger)
        {
            _scannerService = scannerService;
            _logger = logger;
        }

        /// <summary>
        /// Scan a receipt image to extract line items and totals
        /// </summary>
        /// <param name="file">Receipt image file (JPEG, PNG, WEBP)</param>
        /// <param name="language">Language code (fi, en, sv). Default: fi</param>
        /// <param name="provider">AI provider (auto, azure, ollama). Default: auto</param>
        /// <param name="ollamaModel">Ollama model name (only used when provider=ollama)</param>
        /// <returns>Scanned receipt data with line items</returns>
        [HttpPost("scan")]
        [RequestSizeLimit(10_000_000)] // 10MB
        [ProducesResponseType(typeof(ScannedReceiptResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ScannedReceiptResult>> ScanReceipt(
            IFormFile file,
            [FromQuery] string language = "fi",
            [FromQuery] string provider = "auto",
            [FromQuery] string? ollamaModel = null)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { error = "Invalid file type. Only JPEG, PNG, and WEBP images are supported." });
                }

                // Validate file size (10MB max)
                if (file.Length > 10_000_000)
                {
                    return BadRequest(new { error = "File too large. Maximum size is 10MB." });
                }

                _logger.LogInformation("Scanning receipt: {FileName}, Size: {Size} bytes, Provider: {Provider}",
                    file.FileName, file.Length, provider);

                // Read file to byte array
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var imageData = ms.ToArray();

                // Call receipt scanner service
                var result = await _scannerService.ScanReceiptAsync(
                    imageData,
                    language,
                    provider,
                    ollamaModel);

                _logger.LogInformation("Receipt scanned successfully: {LineCount} lines found, Total: {Total}",
                    result.Lines.Count, result.Total);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning receipt");
                return StatusCode(500, new
                {
                    error = "Failed to scan receipt",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Health check endpoint to verify the ReceiptScanner service is accessible
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "ReceiptsController",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
