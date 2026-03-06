using System;

namespace Application.Services
{
    /// <summary>
    /// Service for generating Finnish virtual barcodes and reference numbers
    /// </summary>
    public class VirtualBarcodeService
    {
        /// <summary>
        /// Generates Finnish virtual barcode (version 5 - required by OP and other banks)
        /// Version 5: 54 chars with check digits (required by modern banking systems)
        /// </summary>
        /// <param name="iban">Finnish IBAN (e.g., FI88 0001 2003 1360 00)</param>
        /// <param name="amount">Amount in euros</param>
        /// <param name="referenceNumber">Base reference number (without check digit)</param>
        /// <param name="dueDate">Due date</param>
        /// <returns>54 character virtual barcode string</returns>
        public string GenerateVirtualBarcode(string iban, decimal amount, string referenceNumber, DateTime dueDate)
        {
            if (string.IsNullOrEmpty(iban))
                throw new ArgumentException("IBAN cannot be null or empty", nameof(iban));

            // Always use Version 5 (OP and other banks require 54-57 characters)
            return GenerateVirtualBarcodeV5(iban, amount, referenceNumber, dueDate);
        }

        /// <summary>
        /// Generates Finnish virtual barcode version 4 (more common, for smaller amounts)
        /// Format: Version(1) + IBAN(16) + Amount(6) + Reserved(3) + Reference(20) + DueDate(6) = 52 chars
        /// </summary>
        private string GenerateVirtualBarcodeV4(string iban, decimal amount, string referenceNumber, DateTime dueDate)
        {
            // Version 4 (more common, no check digits)
            string version = "4";

            // IBAN without FI and spaces (16 digits)
            string ibanDigits = iban
                .Replace("FI", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .Replace("-", "")
                .Trim();

            if (ibanDigits.Length > 16)
                ibanDigits = ibanDigits.Substring(0, 16);

            ibanDigits = ibanDigits.PadLeft(16, '0');

            // Amount in cents, 6 digits (max 999999 = 9999.99 EUR)
            // Use rounding to avoid truncation of fractional cents
            long amountCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
            if (amountCents > 999999)
                throw new ArgumentException("Amount too large for version 4 (max 9999.99 EUR)", nameof(amount));

            string amountStr = amountCents.ToString().PadLeft(6, '0');

            // Reserved 3 zeros
            string reserved = "000";

            // Reference number with check digit, padded to 20 digits
            string refWithCheck = GenerateReferenceNumberWithCheckDigit(referenceNumber);
            refWithCheck = refWithCheck.PadLeft(20, '0');

            // Due date YYMMDD
            string dueDateStr = dueDate.ToString("yyMMdd");

            // Build barcode (52 chars, NO check digits for version 4)
            var finalBarcode = $"{version}{ibanDigits}{amountStr}{reserved}{refWithCheck}{dueDateStr}";

            // Log each component for debugging
            System.Diagnostics.Debug.WriteLine($"Virtual Barcode V4 Components:");
            System.Diagnostics.Debug.WriteLine($"  Version: {version} (length: {version.Length})");
            System.Diagnostics.Debug.WriteLine($"  IBAN: {ibanDigits} (length: {ibanDigits.Length})");
            System.Diagnostics.Debug.WriteLine($"  Amount: {amountStr} (length: {amountStr.Length}, original: {amount:F2})");
            System.Diagnostics.Debug.WriteLine($"  Reserved: {reserved} (length: {reserved.Length})");
            System.Diagnostics.Debug.WriteLine($"  Reference: {refWithCheck} (length: {refWithCheck.Length})");
            System.Diagnostics.Debug.WriteLine($"  Due Date: {dueDateStr} (length: {dueDateStr.Length})");
            System.Diagnostics.Debug.WriteLine($"  Final Barcode: {finalBarcode} (length: {finalBarcode.Length})");

            return finalBarcode;
        }

        /// <summary>
        /// Generates Finnish virtual barcode version 5 (for larger amounts)
        /// Format: Version(1) + IBAN(16) + Amount(8) + Reference(20) + DueDate(6) + CheckDigits(3) = 54 chars
        /// </summary>
        private string GenerateVirtualBarcodeV5(string iban, decimal amount, string referenceNumber, DateTime dueDate)
        {
            // Version 4 format (8-digit amount, 2 check digits) - used by Finnish banks
            string version = "4";

            // IBAN without FI and spaces (16 digits)
            string ibanDigits = iban
                .Replace("FI", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .Replace("-", "")
                .Trim();

            if (ibanDigits.Length > 16)
                ibanDigits = ibanDigits.Substring(0, 16);

            ibanDigits = ibanDigits.PadLeft(16, '0');

            // Amount in cents, 8 digits (e.g., 238.92 € = 00023892)
            // Use rounding to avoid truncation of fractional cents
            long amountCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
            string amountStr = amountCents.ToString().PadLeft(8, '0');

            // Reference number with check digit, padded to 23 digits (Finnish v4 standard)
            string refWithCheck = GenerateReferenceNumberWithCheckDigit(referenceNumber);
            refWithCheck = refWithCheck.PadLeft(23, '0');

            // Due date YYMMDD
            string dueDateStr = dueDate.ToString("yyMMdd");

            // Build final barcode (54 chars: 1+16+8+23+6 = 54)
            // Version 4: NO separate check digits at end (reference already contains check digit)
            var finalBarcode = $"{version}{ibanDigits}{amountStr}{refWithCheck}{dueDateStr}";

            // Log each component for debugging
            System.Diagnostics.Debug.WriteLine($"Virtual Barcode V4 (Finnish Standard) Components:");
            System.Diagnostics.Debug.WriteLine($"  Version: {version} (length: {version.Length})");
            System.Diagnostics.Debug.WriteLine($"  IBAN: {ibanDigits} (length: {ibanDigits.Length})");
            System.Diagnostics.Debug.WriteLine($"  Amount: {amountStr} (length: {amountStr.Length}, original: {amount:F5})");
            System.Diagnostics.Debug.WriteLine($"  Reference: {refWithCheck} (length: {refWithCheck.Length})");
            System.Diagnostics.Debug.WriteLine($"  Due Date: {dueDateStr} (length: {dueDateStr.Length})");
            System.Diagnostics.Debug.WriteLine($"  Final Barcode: {finalBarcode} (length: {finalBarcode.Length})");

            return finalBarcode;
        }

        /// <summary>
        /// Calculates 3 check digits for Finnish virtual barcode
        /// Uses modulo 103 algorithm (OLD - not used anymore)
        /// </summary>
        private string CalculateBarcodeCheckDigits(string barcode)
        {
            // Convert string to number and calculate modulo 103
            long sum = 0;
            for (int i = 0; i < barcode.Length; i++)
            {
                sum = (sum * 10 + (barcode[i] - '0')) % 103;
            }

            return sum.ToString("D3");
        }

        /// <summary>
        /// Calculates 3 check digits for Finnish virtual barcode using modulo 103
        /// This is the correct algorithm used by Finnish banks (version 4)
        /// </summary>
        private string CalculateBarcodeCheckDigitsModulo97(string barcode)
        {
            // Use modulo 103 for 3-digit check (Finnish standard)
            long sum = 0;
            for (int i = 0; i < barcode.Length; i++)
            {
                sum = (sum * 10 + (barcode[i] - '0')) % 103;
            }

            return sum.ToString("D3");
        }

        /// <summary>
        /// Generates Finnish reference number with check digit
        /// Uses the Finnish standard for reference number validation
        /// </summary>
        /// <param name="baseNumber">Base number (without check digit)</param>
        /// <returns>Reference number with check digit appended</returns>
        public string GenerateReferenceNumberWithCheckDigit(string baseNumber)
        {
            if (string.IsNullOrEmpty(baseNumber))
                throw new ArgumentException("Base number cannot be null or empty", nameof(baseNumber));

            // Remove any non-digit characters
            baseNumber = new string(Array.FindAll(baseNumber.ToCharArray(), char.IsDigit));

            if (baseNumber.Length == 0)
                throw new ArgumentException("Base number must contain at least one digit", nameof(baseNumber));

            // Finnish reference number check digit calculation
            int[] weights = { 7, 3, 1 };
            int sum = 0;

            for (int i = 0; i < baseNumber.Length; i++)
            {
                int digit = int.Parse(baseNumber[baseNumber.Length - 1 - i].ToString());
                sum += digit * weights[i % 3];
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return baseNumber + checkDigit;
        }

        /// <summary>
        /// Formats reference number with spaces for readability
        /// E.g., "12345678901" -> "1234 56789 01"
        /// </summary>
        public string FormatReferenceNumber(string referenceNumber)
        {
            if (string.IsNullOrEmpty(referenceNumber))
                return string.Empty;

            // Remove existing spaces
            referenceNumber = referenceNumber.Replace(" ", "");

            // Add spaces from right to left in groups of 5
            var result = new System.Text.StringBuilder();
            int count = 0;

            for (int i = referenceNumber.Length - 1; i >= 0; i--)
            {
                if (count > 0 && count % 5 == 0)
                    result.Insert(0, ' ');

                result.Insert(0, referenceNumber[i]);
                count++;
            }

            return result.ToString();
        }

        /// <summary>
        /// Generates a simple reference number from invoice ID
        /// </summary>
        public string GenerateReferenceFromInvoiceId(Guid invoiceId)
        {
            // Use last 12 digits of invoice GUID hash
            var hash = Math.Abs(invoiceId.GetHashCode());
            var baseNumber = hash.ToString().PadLeft(12, '0');

            if (baseNumber.Length > 12)
                baseNumber = baseNumber.Substring(baseNumber.Length - 12);

            return baseNumber;
        }
    }
}
