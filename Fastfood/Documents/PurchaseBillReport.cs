using Fastfood.Data;
using Fastfood.Models;
using Fastfood.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

public class PurchaseBillReport : IDocument
{
    private readonly Inv_Purchase _purchase;
    private readonly List<Inv_PurchasedItems> _items;
    private readonly string _logoPath;

    public PurchaseBillReport(Inv_Purchase purchase, List<Inv_PurchasedItems> items, string logoPath)
    {
        _purchase = purchase;
        _items = items;
        _logoPath = logoPath;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);

            // HEADER
            page.Header().Column(header =>
            {
                header.Spacing(5);

                // Top row: Invoice title + logo
             header.Item().Row(row =>
{
    row.RelativeItem()
        .Text("INVOICE")
        .Bold()
        .FontSize(26)
        .FontColor(Colors.Black);

    if (!string.IsNullOrEmpty(_logoPath))
    {
        row.ConstantItem(80) // smaller container width
            .Height(50)      // optional: control height
            .Image(_logoPath)
            .FitHeight();    // fit within the height without stretching
    }
});

                // Company Info
                header.Item().Column(col =>
                {
                    col.Spacing(2);
                    col.Item().Text("CLean Serve").SemiBold().FontSize(12);
                    col.Item().Text("University OF Kotli Azad Kashmir").FontSize(12);
                    col.Item().Text("Kotli, Kotli, 11100").FontSize(12);
                    col.Item().Text("Phone: (92) 3401159116").FontSize(12);
                });

                // Billing and Purchase Info
                header.Item().Row(row =>
                {
                    // Bill To / Ship To
                    row.RelativeItem().Column(col =>
                    {
                        col.Spacing(2);
                        col.Item().Text("BILL TO").SemiBold().FontSize(12);
                        col.Item().Text(_purchase.Supplier?.Name ?? "Supplier Name");
                        col.Item().Text(_purchase.Supplier?.Address ?? "Supplier Address");

                        col.Item().PaddingTop(10).Text("SHIP TO").SemiBold().FontSize(12);
                        col.Item().Text((_purchase.Supplier?.Name.ToString()) ?? "Supplier ID");
                        col.Item().Text(_purchase.Supplier?.Address ?? "Supplier Address");
                    });

                    // Invoice Info
                    row.ConstantItem(200).Column(col =>
                    {
                        col.Spacing(2);
                        col.Item().Text($"Invoice #: {_purchase.InvoiceNo}");
                        col.Item().Text($"Invoice Date: {_purchase.PurchaseDate?.ToString("dd/MM/yyyy") ?? ""}");
                    });
                });


            });

            // CONTENT TABLE
            page.Content().PaddingVertical(20).Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(6);   // DESCRIPTION - wide
                        columns.ConstantColumn(35);  // QTY - narrow
                        columns.ConstantColumn(80);  // UNIT PRICE - close to QTY
                        columns.ConstantColumn(60);  // AMOUNT - right side
                    });

                    // Header row with red line
                    table.Header(header =>
                    {
                        header.Cell().Text("Products").SemiBold().FontColor(Colors.Black);
                        header.Cell().AlignRight().Text("QTY").SemiBold().FontColor(Colors.Black);
                        header.Cell().AlignRight().Text("UNIT PRICE").SemiBold().FontColor(Colors.Black);
                        header.Cell().AlignRight().Text("AMOUNT").SemiBold().FontColor(Colors.Black);

                        header.Cell().ColumnSpan(4)
                            .BorderBottom(1)
                            .BorderColor(Colors.Red.Medium)
                            .PaddingVertical(2);
                    });

                    // Items with spacing
                    foreach (var item in _items)
                    {
                        // Wrap each row in a small padding block for spacing
                        table.Cell().PaddingBottom(4).Text(item.ItemName);
                        table.Cell().PaddingBottom(4).AlignRight().Text(item.Qty.ToString());
                        table.Cell().PaddingBottom(4).AlignRight().Text(item.UnitPrice.ToString("0.00"));
                        table.Cell().PaddingBottom(4).AlignRight().Text((item.UnitPrice * Convert.ToDecimal(item.Qty)).ToString("0.00"));
                    }
                });

                // Totals
                decimal subtotal = _items.Sum(x => x.UnitPrice * Convert.ToDecimal(x.Qty));
                decimal tax = subtotal * 0.0625m; // 6.25% example
                decimal total = subtotal + tax;

                col.Item().PaddingTop(10).AlignRight().Column(totalCol =>
                {
                    totalCol.Spacing(2);
                    totalCol.Item().Text($"Subtotal: {subtotal:0.00}");
                    totalCol.Item().Text($"Sales Tax 6.25%: {tax:0.00}");
                    totalCol.Item().Text($"TOTAL: {total:0.00}").Bold();
                });
            });

            // FOOTER
            page.Footer().Column(footer =>
            {
                footer.Spacing(5);

                footer.Item().Row(row =>
                {
                    row.RelativeItem().Text("Thank you").FontSize(24).Bold();
                    row.ConstantItem(200).Column(col =>
                    {
                        col.Spacing(2);
                        col.Item().Text("TERMS & CONDITIONS").SemiBold().FontColor(Colors.Red.Medium);
                        col.Item().Text("Payment is due within 15 days");
                        col.Item().Text("Please make checks payable to: CLean Serve");
                    });
                });

                footer.Item().AlignRight().PaddingTop(20).Text("Signature: ___________________");
            });
        });
    }
}
