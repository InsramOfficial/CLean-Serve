namespace Fastfood.Documents
{
    using Fastfood.Models;
    using QuestPDF.Fluent;
    using QuestPDF.Helpers;
    using QuestPDF.Infrastructure;
    using System.Collections.Generic;
    using System.Linq;

    public class CustomerSalesHistoryReport : IDocument
    {
        private readonly Client _customer;
        private readonly List<Sales> _sales;
        private readonly string _logoPath;

        public CustomerSalesHistoryReport(
            Client customer,
            List<Sales> sales,
            string logoPath)
        {
            _customer = customer;
            _sales = sales;
            _logoPath = logoPath;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // HEADER
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Customer Sales History")
                            .Bold()
                            .FontSize(20)
                            .FontColor(Colors.Blue.Medium);

                        col.Item().Text($"Customer: {_customer.Name}");
                        col.Item().Text($"Phone: {_customer.PhoneNo}");
                        col.Item().Text($"Address: {_customer.Address}");
                    });

                    if (!string.IsNullOrEmpty(_logoPath))
                    {
                        // FIX: Changed .FitHeight() to .FitArea() 
                        // to prevent the width from expanding beyond 80 units
                        row.ConstantItem(80)
                           .Image(_logoPath)
                           .FitArea();
                    }
                });

                // CONTENT
                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(90);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    // HEADER ROW
                    table.Header(header =>
                    {
                        // Helper to apply consistent header styling
                        IContainer HeaderStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(5);

                        header.Cell().Element(HeaderStyle).Text("Date").SemiBold();
                        header.Cell().Element(HeaderStyle).Text("Serving").SemiBold();
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Payment").SemiBold();
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Total").SemiBold();
                    });

                    // DATA ROWS
                    foreach (var sale in _sales)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text(sale.SaleDate?.ToString("dd/MM/yyyy"));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text(sale.Serving ?? "N/A");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignRight().Text(sale.Payment?.ToString("0.00") ?? "0.00");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignRight().Text(sale.Cash_Received?.ToString("0.00") ?? "0.00");
                    }
                });

                // FOOTer
                page.Footer().Column(col =>
                {
                    var total = _sales.Sum(x => x.Cash_Received ?? 0);

                    col.Item().PaddingTop(10).AlignRight().Text($"Total Sales: {total:0.00}")
                        .Bold()
                        .FontSize(12);

                    col.Item().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });
        }
    }
}