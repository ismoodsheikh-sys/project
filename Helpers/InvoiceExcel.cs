using VehicleFleetMS.Models;

namespace VehicleFleetMS.Helpers;

/// <summary>Renders a one-sheet fuel purchase invoice as an .xlsx workbook.</summary>
public static class InvoiceExcel
{
    public static byte[] Build(AppSettings settings, FuelRecord record, string invoiceNumber)
    {
        List<ExcelCell> Row(params ExcelCell[] cells) => [.. cells];

        var rows = new List<IEnumerable<ExcelCell>>
        {
            Row(new ExcelCell(settings.AgencyName, Bold: true)),
            Row(settings.DivisionName),
            Row(settings.HeadquartersAddress),
            Row($"{settings.ContactEmail} · {settings.ContactPhone}"),
            Row(),
            Row(new ExcelCell("INVOICE", Bold: true), "", new ExcelCell("Invoice #", Bold: true), invoiceNumber),
            Row(new ExcelCell("Bill To", Bold: true), "", new ExcelCell("Invoice date", Bold: true), DateTime.UtcNow.ToString("yyyy-MM-dd")),
            Row(new ExcelCell(record.Vehicle?.PlateNumber ?? "—", Bold: true), "", new ExcelCell("Transaction date", Bold: true), record.Date.ToString("yyyy-MM-dd")),
            Row($"{record.Vehicle?.Make} {record.Vehicle?.Model}".Trim(), "", new ExcelCell("Payment method", Bold: true), record.PaymentMethod),
            Row($"Driver: {record.Driver?.Name ?? "Unassigned"}", "", new ExcelCell("Station", Bold: true), record.Station),
            Row($"Depot: {record.Vehicle?.Depot}"),
            Row(),
            Row(new ExcelCell("Description", Bold: true), new ExcelCell("Qty (L)", Bold: true), new ExcelCell("Unit Price", Bold: true), new ExcelCell("Amount", Bold: true)),
            Row($"{record.FuelType} fuel purchase — {record.Station}", record.Liters, record.CostPerLiter, record.TotalCost),
            Row(),
            Row("", "", new ExcelCell("Subtotal", Bold: true), record.TotalCost),
            Row("", "", new ExcelCell("Tax", Bold: true), 0),
            Row("", "", new ExcelCell("Total Due", Bold: true), new ExcelCell(record.TotalCost, Bold: true)),
            Row(),
            Row($"Odometer reading at fill-up: {record.Odometer:N0} km"),
        };

        return ExcelExport.BuildFreeform("Invoice", rows);
    }
}
