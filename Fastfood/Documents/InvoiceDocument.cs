using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System;

namespace Fastfood.Documents
{
    public class InvoiceDocument : IDocument
    {
        public string InvoiceNo { get; set; }
        public string DealingPerson { get; set; }
        public string CustomerName { get; set; }
        public string Serving { get; set; }
        public double TotalAmount { get; set; }
        public double CashReceived { get; set; }
        public double ChangeBack { get; set; }
        public List<dynamic> Items { get; set; }
        public string LogoPath { get; set; }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            float receiptWidth = 80 * 2.83f; // 80mm in points (~226.4)

            container.Page(page =>
            {
                page.Size(receiptWidth, 3000); // Long scrolling receipt
                page.Margin(10);
                page.PageColor(Colors.White);

                page.Content().Column(col =>
                {
                    // ---------- HEADER ----------
                    col.Item().AlignCenter().Text("RECEIPT")
                        .FontSize(14)
                        .Bold();

                    col.Item().PaddingVertical(4)
                       .AlignCenter().Text("--------------------------------------").FontSize(9);

                    col.Item().AlignCenter().Text($"Invoice #: {InvoiceNo}")
                        .FontSize(9);
                    col.Item().AlignCenter().Text($"Date: {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                        .FontSize(9);
                    col.Item().AlignCenter().Text($"By: {DealingPerson}")
                        .FontSize(9);
                    col.Item().AlignCenter().Text($"Customer: {CustomerName}")
                        .FontSize(9);
                    col.Item().AlignCenter().Text($"Serving: {Serving}")
                        .FontSize(9);

                    col.Item().PaddingVertical(4)
                        .AlignCenter().Text("--------------------------------------").FontSize(9);

                    // ---------- ITEM LIST ----------
                    col.Item().Column(itemCol =>
                    {
                        foreach (var item in Items)
                        {
                            decimal price = Convert.ToDecimal(item.UnitPrice);
                            decimal qty = Convert.ToDecimal(item.Qty);
                            decimal total = price * qty;

                            itemCol.Item().Row(row =>
                            {
                                row.RelativeColumn().Text($"{qty}x {item.ItemName}")
                                    .FontSize(9);

                                row.ConstantColumn(60)
                                    .AlignRight()
                                    .Text($"Rs. {total:0.00}")
                                    .FontSize(9);
                            });
                        }
                    });

                    col.Item().PaddingVertical(4)
                       .AlignCenter().Text("--------------------------------------").FontSize(9);

                    // ---------- TOTALS ----------
                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Text("TOTAL AMOUNT").Bold().FontSize(10);
                        row.ConstantColumn(70).AlignRight().Text($"Rs. {TotalAmount:0.00}").Bold().FontSize(10);
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Text("CASH").FontSize(9);
                        row.ConstantColumn(70).AlignRight().Text($"Rs. {CashReceived:0.00}").FontSize(9);
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Text("CHANGE").FontSize(9);
                        row.ConstantColumn(70).AlignRight().Text($"Rs. {ChangeBack:0.00}").FontSize(9);
                    });

                    col.Item().PaddingVertical(4)
                       .AlignCenter().Text("--------------------------------------").FontSize(9);

                    // ---------- FOOTER ----------
                    col.Item().AlignCenter().PaddingTop(10)
                        .Text("THANK YOU")
                        .FontSize(11)
                        .Bold();

                    col.Item().AlignCenter().PaddingTop(5)
                        .Text("|||||||||||||||||||||||||||||||||||||||||||")
                        .FontSize(9);

                    col.Item().AlignCenter().PaddingTop(5)
                        .Text("Powered by Clean Serve")
                        .FontSize(7)
                        .FontColor(Colors.Grey.Darken2);
                });
            });
        }
    }
}
