using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IReceiptScannerService
    {
        Task<ScannedReceiptResult> ScanReceiptAsync(
            byte[] imageData,
            string language = "fi",
            string provider = "auto",
            string? ollamaModel = null);
    }

    public record ScannedReceiptResult
    {
        public string Provider { get; init; } = "";
        public List<ScannedReceiptLine> Lines { get; init; } = new();
        public decimal? Total { get; init; }
        public string? MerchantName { get; init; }
        public DateTime? ReceiptDate { get; init; }
        public List<string> Warnings { get; init; } = new();
        public long ProcessingTimeMs { get; init; }
    }

    public record ScannedReceiptLine
    {
        public string Description { get; init; } = "";
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
        public decimal? Confidence { get; init; }
    }
}
