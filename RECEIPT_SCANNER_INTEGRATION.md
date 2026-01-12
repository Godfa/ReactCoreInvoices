# Receipt Scanner Integration Plan

## Overview

This document outlines how MokkilanInvoices will integrate with a future Receipt Scanner microservice. The scanner will be a separate service that extracts line items from receipt images using Azure AI Document Intelligence and/or Ollama vision models.

## Current Architecture Understanding

### Data Flow: Receipt → Expense → Line Items

```
Invoice
 └── ExpenseItem (expense category, organizer, payers)
      └── ExpenseLineItem[] (name, quantity, unitPrice → total)
           └── Automatically computes ExpenseItem.Amount
```

### Key Entities

- **ExpenseItem**: Container for an expense (e.g., "Grocery shopping at K-Market")
  - OrganizerId: Who paid
  - ExpenseType: ShoppingList | Gasoline | Personal
  - Name: Description
  - Amount: Computed from LineItems

- **ExpenseLineItem**: Individual items from receipt
  - Name: "MILK 1L", "BREAD", etc.
  - Quantity: int
  - UnitPrice: decimal
  - Total: Quantity × UnitPrice (computed)

- **ExpenseItemPayer**: Who owes money for this expense (junction table)

## Receipt Scanner Service (External)

### Service Responsibility

Standalone microservice that:
1. Accepts receipt image
2. Extracts structured data using vision AI
3. Returns normalized JSON response
4. Handles provider selection (Azure/Ollama/Composite)

### API Contract

**Endpoint**: `POST https://scanner.example.com/api/receipts/scan`

**Request**:
```json
{
  "imageBase64": "...",
  "language": "fi",
  "provider": "auto" // or "azure", "ollama"
}
```

**Response**:
```json
{
  "provider": "azure",
  "lines": [
    {
      "description": "MILK 1L",
      "quantity": 1,
      "unitPrice": 1.29,
      "lineTotal": 1.29
    },
    {
      "description": "BREAD",
      "quantity": 2,
      "unitPrice": 2.50,
      "lineTotal": 5.00
    }
  ],
  "total": 6.29,
  "rawText": null,
  "warnings": ["low_confidence_on_item_2"]
}
```

## Integration Points in MokkilanInvoices

### 1. New DTOs for Receipt Scanning

**Location**: `API/DTOs/ReceiptScanDto.cs`

```csharp
// Request to scanner service
public record ScanReceiptRequest
{
    public string ImageBase64 { get; init; } = "";
    public string Language { get; init; } = "fi";
    public string Provider { get; init; } = "auto";
}

// Response from scanner service
public record ScannedReceiptLine
{
    public string Description { get; init; } = "";
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; } = 0;
    public decimal LineTotal { get; init; } = 0;
}

public record ScannedReceiptResult
{
    public string Provider { get; init; } = "";
    public List<ScannedReceiptLine> Lines { get; init; } = new();
    public decimal? Total { get; init; }
    public string? RawText { get; init; }
    public List<string> Warnings { get; init; } = new();
}

// DTO to create expense from scanned receipt
public record CreateExpenseFromReceiptDto
{
    public Guid InvoiceId { get; init; }
    public string OrganizerId { get; init; } = "";
    public ExpenseType ExpenseType { get; init; }
    public string Name { get; init; } = ""; // e.g., "K-Market 12.1.2026"
    public string? ImageUrl { get; init; } // Optional: store receipt image URL
    public List<ScannedReceiptLine> Lines { get; init; } = new();
}
```

### 2. Application Handler

**Location**: `Application/Receipts/ScanAndCreateExpense.cs`

```csharp
namespace Application.Receipts
{
    public class ScanAndCreateExpense
    {
        public class Command : IRequest<ExpenseItem>
        {
            public Guid InvoiceId { get; set; }
            public string OrganizerId { get; set; } = "";
            public ExpenseType ExpenseType { get; set; }
            public string Name { get; set; } = "";
            public IFormFile ReceiptImage { get; set; } = null!;
            public string Provider { get; set; } = "auto";
        }

        public class Handler : IRequestHandler<Command, ExpenseItem>
        {
            private readonly DataContext _context;
            private readonly IReceiptScannerService _scanner;

            public Handler(DataContext context, IReceiptScannerService scanner)
            {
                _context = context;
                _scanner = scanner;
            }

            public async Task<ExpenseItem> Handle(Command request, CancellationToken ct)
            {
                // 1. Validate invoice exists and is editable
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

                if (invoice == null)
                    throw new Exception("Invoice not found");

                if (invoice.Status != InvoiceStatus.Aktiivinen)
                    throw new Exception("Cannot add expenses to invoice in current status");

                // 2. Call scanner service
                var scanResult = await _scanner.ScanReceiptAsync(
                    request.ReceiptImage,
                    request.Provider,
                    ct);

                // 3. Create ExpenseItem
                var expenseItem = new ExpenseItem
                {
                    Id = Guid.NewGuid(),
                    OrganizerId = request.OrganizerId,
                    ExpenseType = request.ExpenseType,
                    Name = request.Name,
                    LineItems = new List<ExpenseLineItem>()
                };

                // 4. Add LineItems from scan result
                foreach (var line in scanResult.Lines)
                {
                    if (string.IsNullOrWhiteSpace(line.Description))
                        continue;

                    expenseItem.LineItems.Add(new ExpenseLineItem
                    {
                        Id = Guid.NewGuid(),
                        ExpenseItemId = expenseItem.Id,
                        Name = line.Description.Trim(),
                        Quantity = (int)Math.Max(1, Math.Round(line.Quantity)),
                        UnitPrice = line.UnitPrice
                    });
                }

                // 5. Save to database
                _context.ExpenseItems.Add(expenseItem);
                _context.Entry(expenseItem).Property("InvoiceId").CurrentValue = request.InvoiceId;

                await _context.SaveChangesAsync(ct);

                // 6. Add all invoice participants as payers
                var participants = await _context.InvoiceParticipants
                    .Where(p => p.InvoiceId == request.InvoiceId)
                    .ToListAsync(ct);

                foreach (var participant in participants)
                {
                    _context.ExpenseItemPayers.Add(new ExpenseItemPayer
                    {
                        ExpenseItemId = expenseItem.Id,
                        AppUserId = participant.AppUserId
                    });
                }

                await _context.SaveChangesAsync(ct);

                // 7. Reload with relations
                expenseItem = await _context.ExpenseItems
                    .Include(e => e.Organizer)
                    .Include(e => e.LineItems)
                    .Include(e => e.Payers)
                        .ThenInclude(p => p.AppUser)
                    .FirstOrDefaultAsync(e => e.Id == expenseItem.Id, ct);

                return expenseItem!;
            }
        }
    }
}
```

### 3. Service Interface

**Location**: `Application/Interfaces/IReceiptScannerService.cs`

```csharp
namespace Application.Interfaces
{
    public interface IReceiptScannerService
    {
        Task<ScannedReceiptResult> ScanReceiptAsync(
            IFormFile image,
            string provider,
            CancellationToken ct);
    }
}
```

### 4. Service Implementation (HTTP Client)

**Location**: `API/Services/ReceiptScannerService.cs`

```csharp
namespace API.Services
{
    public class ReceiptScannerService : IReceiptScannerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReceiptScannerService> _logger;

        public ReceiptScannerService(HttpClient httpClient, ILogger<ReceiptScannerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ScannedReceiptResult> ScanReceiptAsync(
            IFormFile image,
            string provider,
            CancellationToken ct)
        {
            try
            {
                // Convert image to base64
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms, ct);
                var base64 = Convert.ToBase64String(ms.ToArray());

                // Call external scanner service
                var request = new
                {
                    imageBase64 = base64,
                    language = "fi",
                    provider = provider
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "/api/receipts/scan",
                    request,
                    ct);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ScannedReceiptResult>(
                    cancellationToken: ct);

                return result ?? new ScannedReceiptResult
                {
                    Provider = "unknown",
                    Lines = new List<ScannedReceiptLine>(),
                    Warnings = new List<string> { "empty_response_from_scanner" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan receipt");
                return new ScannedReceiptResult
                {
                    Provider = "error",
                    Lines = new List<ScannedReceiptLine>(),
                    Warnings = new List<string> { $"scanner_error: {ex.Message}" }
                };
            }
        }
    }
}
```

### 5. Controller Endpoint

**Location**: `API/Controllers/ReceiptsController.cs` (new file)

```csharp
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : BaseApiController
    {
        [HttpPost("scan-and-create")]
        [RequestSizeLimit(10_000_000)] // 10MB max
        public async Task<ActionResult<ExpenseItem>> ScanAndCreateExpense(
            [FromForm] Guid invoiceId,
            [FromForm] string organizerId,
            [FromForm] ExpenseType expenseType,
            [FromForm] string name,
            [FromForm] IFormFile receiptImage,
            [FromQuery] string provider = "auto")
        {
            var command = new ScanAndCreateExpense.Command
            {
                InvoiceId = invoiceId,
                OrganizerId = organizerId,
                ExpenseType = expenseType,
                Name = name,
                ReceiptImage = receiptImage,
                Provider = provider
            };

            return await Mediator.Send(command);
        }

        [HttpPost("scan-only")]
        [RequestSizeLimit(10_000_000)]
        public async Task<ActionResult<ScannedReceiptResult>> ScanOnly(
            [FromForm] IFormFile receiptImage,
            [FromQuery] string provider = "auto")
        {
            var scanner = HttpContext.RequestServices
                .GetRequiredService<IReceiptScannerService>();

            var result = await scanner.ScanReceiptAsync(
                receiptImage,
                provider,
                HttpContext.RequestAborted);

            return Ok(result);
        }
    }
}
```

### 6. Configuration

**Location**: `API/appsettings.json`

```json
{
  "ReceiptScanner": {
    "BaseUrl": "https://scanner.example.com",
    "ApiKey": "your-api-key-here",
    "TimeoutSeconds": 60
  }
}
```

**Location**: `API/Program.cs` (DI registration)

```csharp
// Receipt Scanner Service
builder.Services.AddHttpClient<IReceiptScannerService, ReceiptScannerService>((sp, client) =>
{
    var baseUrl = builder.Configuration["ReceiptScanner:BaseUrl"]!;
    var apiKey = builder.Configuration["ReceiptScanner:ApiKey"]!;
    var timeout = int.Parse(builder.Configuration["ReceiptScanner:TimeoutSeconds"] ?? "60");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
});
```

## Frontend Integration

### 1. New TypeScript Types

**Location**: `client-app/src/app/models/receipt.ts`

```typescript
export interface ScannedReceiptLine {
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface ScannedReceiptResult {
  provider: string;
  lines: ScannedReceiptLine[];
  total: number | null;
  rawText: string | null;
  warnings: string[];
}

export interface CreateExpenseFromReceiptFormValues {
  organizerId: string;
  expenseType: number;
  name: string;
  receiptImage: File | null;
  provider: string;
}
```

### 2. API Agent Methods

**Location**: `client-app/src/app/api/agent.ts`

```typescript
const Receipts = {
  scanOnly: (image: File, provider: string = 'auto') => {
    const formData = new FormData();
    formData.append('receiptImage', image);
    return requests.post<ScannedReceiptResult>(
      `/receipts/scan-only?provider=${provider}`,
      formData
    );
  },

  scanAndCreateExpense: (
    invoiceId: string,
    organizerId: string,
    expenseType: number,
    name: string,
    image: File,
    provider: string = 'auto'
  ) => {
    const formData = new FormData();
    formData.append('invoiceId', invoiceId);
    formData.append('organizerId', organizerId);
    formData.append('expenseType', expenseType.toString());
    formData.append('name', name);
    formData.append('receiptImage', image);
    return requests.post<ExpenseItem>(
      `/receipts/scan-and-create?provider=${provider}`,
      formData
    );
  }
};

export default {
  // ... existing exports
  Receipts
};
```

### 3. React Component

**Location**: `client-app/src/features/receipts/ReceiptScannerForm.tsx`

```typescript
import { useState } from 'react';
import { observer } from 'mobx-react-lite';
import { Formik, Form } from 'formik';
import * as Yup from 'yup';
import { Button, Segment, Header, Message, Table } from 'semantic-ui-react';
import MyTextInput from '../../app/common/form/MyTextInput';
import MySelectInput from '../../app/common/form/MySelectInput';
import { useStore } from '../../app/stores/store';
import agent from '../../app/api/agent';
import { ScannedReceiptResult } from '../../app/models/receipt';

interface Props {
  invoiceId: string;
}

export default observer(function ReceiptScannerForm({ invoiceId }: Props) {
  const { invoiceStore } = useStore();
  const [scanning, setScanning] = useState(false);
  const [scanResult, setScanResult] = useState<ScannedReceiptResult | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const expenseTypeOptions = [
    { text: 'Shopping List', value: 0 },
    { text: 'Gasoline', value: 1 },
    { text: 'Personal', value: 2 }
  ];

  const providerOptions = [
    { text: 'Auto (Fallback)', value: 'auto' },
    { text: 'Azure AI', value: 'azure' },
    { text: 'Ollama', value: 'ollama' }
  ];

  const validationSchema = Yup.object({
    organizerId: Yup.string().required('Organizer is required'),
    expenseType: Yup.number().required('Expense type is required'),
    name: Yup.string().required('Name is required'),
    provider: Yup.string().required()
  });

  const handleScanPreview = async () => {
    if (!selectedFile) return;

    setScanning(true);
    try {
      const result = await agent.Receipts.scanOnly(selectedFile, 'auto');
      setScanResult(result);
    } catch (error) {
      console.error('Scan failed:', error);
    } finally {
      setScanning(false);
    }
  };

  const handleSubmit = async (values: any) => {
    if (!selectedFile) return;

    try {
      await agent.Receipts.scanAndCreateExpense(
        invoiceId,
        values.organizerId,
        values.expenseType,
        values.name,
        selectedFile,
        values.provider
      );

      // Reload invoice to get updated expense
      await invoiceStore.loadInvoice(invoiceId);

      // Close form or show success
      invoiceStore.closeForm();
    } catch (error) {
      console.error('Failed to create expense:', error);
    }
  };

  return (
    <Segment clearing>
      <Header content="Scan Receipt and Create Expense" color="teal" />

      <Formik
        initialValues={{
          organizerId: '',
          expenseType: 0,
          name: '',
          provider: 'auto'
        }}
        validationSchema={validationSchema}
        onSubmit={handleSubmit}
      >
        {({ handleSubmit, isSubmitting, values }) => (
          <Form className="ui form" onSubmit={handleSubmit}>

            {/* File Upload */}
            <div className="field">
              <label>Receipt Image</label>
              <input
                type="file"
                accept="image/*"
                onChange={(e) => setSelectedFile(e.target.files?.[0] || null)}
              />
            </div>

            {selectedFile && (
              <Button
                type="button"
                content="Preview Scan"
                onClick={handleScanPreview}
                loading={scanning}
                disabled={scanning}
                color="blue"
              />
            )}

            <MyTextInput name="name" placeholder="Expense Name (e.g., K-Market 12.1.2026)" />

            <MySelectInput
              name="organizerId"
              placeholder="Organizer (who paid)"
              options={invoiceStore.organizerOptions}
            />

            <MySelectInput
              name="expenseType"
              placeholder="Expense Type"
              options={expenseTypeOptions}
            />

            <MySelectInput
              name="provider"
              placeholder="Scanner Provider"
              options={providerOptions}
            />

            {/* Scan Result Preview */}
            {scanResult && (
              <Segment color="green">
                <Header size="small">Scanned Items ({scanResult.lines.length})</Header>
                {scanResult.warnings.length > 0 && (
                  <Message warning list={scanResult.warnings} />
                )}
                <Table celled>
                  <Table.Header>
                    <Table.Row>
                      <Table.HeaderCell>Item</Table.HeaderCell>
                      <Table.HeaderCell>Qty</Table.HeaderCell>
                      <Table.HeaderCell>Price</Table.HeaderCell>
                      <Table.HeaderCell>Total</Table.HeaderCell>
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {scanResult.lines.map((line, idx) => (
                      <Table.Row key={idx}>
                        <Table.Cell>{line.description}</Table.Cell>
                        <Table.Cell>{line.quantity}</Table.Cell>
                        <Table.Cell>€{line.unitPrice.toFixed(2)}</Table.Cell>
                        <Table.Cell>€{line.lineTotal.toFixed(2)}</Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                  <Table.Footer>
                    <Table.Row>
                      <Table.HeaderCell colSpan="3">Total</Table.HeaderCell>
                      <Table.HeaderCell>
                        €{scanResult.total?.toFixed(2) || 'N/A'}
                      </Table.HeaderCell>
                    </Table.Row>
                  </Table.Footer>
                </Table>
              </Segment>
            )}

            <Button
              disabled={!selectedFile || isSubmitting}
              loading={isSubmitting}
              floated="right"
              positive
              type="submit"
              content="Create Expense"
            />
            <Button
              disabled={isSubmitting}
              floated="right"
              type="button"
              content="Cancel"
              onClick={() => invoiceStore.closeForm()}
            />
          </Form>
        )}
      </Formik>
    </Segment>
  );
});
```

## Migration Path

### Phase 1: Preparation (Current State)
- ✅ ExpenseLineItem structure already exists
- ✅ All necessary relationships in place
- 📝 Document integration plan (this file)

### Phase 2: Mock Implementation (Dev/Testing)
1. Create stub `IReceiptScannerService` that returns fake data
2. Implement all backend DTOs and handlers
3. Build frontend UI with mock responses
4. Test end-to-end flow with fake data

### Phase 3: External Service Integration
1. Deploy Receipt Scanner microservice separately
2. Configure `appsettings.json` with real scanner URL
3. Replace mock service with `ReceiptScannerService` HTTP client
4. Test with real Azure/Ollama responses

### Phase 4: Production Rollout
1. Add feature flag to enable/disable scanner
2. Monitor performance and accuracy
3. Collect user feedback
4. Iterate on UI/UX based on real-world usage

## Testing Strategy

### Unit Tests
- `ReceiptNormalizer`: Validates and cleans scanned data
- `ScanAndCreateExpense.Handler`: Business logic with mocked scanner service

### Integration Tests
- Mock HTTP responses from scanner service
- Verify ExpenseItem + LineItems created correctly
- Test error handling (scanner timeout, invalid data, etc.)

### E2E Tests
- Upload real receipt images
- Verify UI displays scanned items
- Confirm expense saves with correct line items

## Security Considerations

1. **File Upload Limits**: Max 10MB per receipt
2. **Authentication**: Bearer token for scanner service
3. **Rate Limiting**: Prevent abuse of scanner API
4. **Input Validation**: Sanitize file types (allow only images)
5. **Error Handling**: Never expose scanner service internals to client

## Performance Considerations

1. **Async Processing**: Scanner calls can take 5-60 seconds
   - Show loading spinner in UI
   - Consider webhook callback for very slow scans

2. **Caching**: Cache scan results temporarily (5-10 minutes)
   - Allow user to retry without re-scanning

3. **Timeout Handling**: 60-second timeout, fallback to manual entry

## Future Enhancements

1. **Edit Before Save**: Allow user to modify scanned line items before creating expense
2. **Confidence Scores**: Display low-confidence items highlighted for review
3. **Receipt Image Storage**: Save receipt image with expense for audit trail
4. **Bulk Scanning**: Upload multiple receipts at once
5. **Template Learning**: Train on user corrections to improve accuracy
6. **Multi-language**: Support receipts in different languages (currently fi)

## Summary

This integration plan enables MokkilanInvoices to:
- ✅ Accept receipt images from users
- ✅ Extract line items automatically using AI
- ✅ Create ExpenseItems with pre-filled LineItems
- ✅ Maintain separation of concerns (scanner as external service)
- ✅ Support multiple vision providers (Azure, Ollama, A/B testing)
- ✅ Gracefully handle errors and low-confidence results

The architecture is designed to be flexible, testable, and maintainable while keeping the scanner service completely decoupled from the core invoice management system.
