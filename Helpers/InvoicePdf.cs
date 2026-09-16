using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Helpers;

/// <summary>Renders a one-page fuel purchase invoice as a PDF via QuestPDF.</summary>
public static class InvoicePdf
{
    public static byte[] Build(AppSettings settings, FuelRecord record, string invoiceNumber)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(settings.AgencyName).FontSize(16).Bold();
                        col.Item().Text(settings.DivisionName).FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(settings.HeadquartersAddress).FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"{settings.ContactEmail}  ·  {settings.ContactPhone}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(160).Column(col =>
                    {
                        col.Item().AlignRight().Text("INVOICE").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().AlignRight().Text(invoiceNumber).FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILL TO").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().Text(record.Vehicle?.PlateNumber ?? "—").FontSize(12).Bold();
                            c.Item().Text($"{record.Vehicle?.Make} {record.Vehicle?.Model}".Trim());
                            c.Item().Text($"Driver: {record.Driver?.Name ?? "Unassigned"}");
                            c.Item().Text($"Depot: {record.Vehicle?.Depot}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Invoice date: {DateTime.UtcNow:MMM d, yyyy}");
                            c.Item().AlignRight().Text($"Transaction date: {record.Date:MMM d, yyyy}");
                            c.Item().AlignRight().Text($"Payment method: {record.PaymentMethod}");
                            c.Item().AlignRight().Text($"Station: {record.Station}");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Description");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Qty (L)");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit price");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                        });

                        table.Cell().Element(DataCell).Text($"{record.FuelType} fuel purchase — {record.Station}");
                        table.Cell().Element(DataCell).AlignRight().Text(record.Liters.ToString("0.0"));
                        table.Cell().Element(DataCell).AlignRight().Text($"${record.CostPerLiter:0.00}");
                        table.Cell().Element(DataCell).AlignRight().Text($"${record.TotalCost:0.00}");
                    });

                    col.Item().AlignRight().Width(220).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotal");
                            r.ConstantItem(80).AlignRight().Text($"${record.TotalCost:0.00}");
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Tax");
                            r.ConstantItem(80).AlignRight().Text("$0.00");
                        });
                        c.Item().PaddingTop(6).BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Total due").Bold();
                            r.ConstantItem(80).AlignRight().Text($"${record.TotalCost:0.00}").Bold();
                        });
                    });

                    col.Item().PaddingTop(10).Text($"Odometer reading at fill-up: {record.Odometer:N0} km").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by VMS Fleet Operations · ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.Bold().FontColor(Colors.White)).Background(Colors.Blue.Darken2).Padding(6);

    private static IContainer DataCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(8).PaddingHorizontal(6);
}
