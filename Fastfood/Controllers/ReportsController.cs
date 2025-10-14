using AspNetCore.Reporting;
using Fastfood.Data;
using Fastfood.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;  
using QuestPDF.Infrastructure;
namespace Fastfood.Controllers
{
    public class ReportsController : Controller
    {
        private readonly DataDbContext db;
        private IWebHostEnvironment env;
        public ReportsController(DataDbContext _db, IWebHostEnvironment _env)
        {
            db = _db;
            env = _env;
        }


        public async Task<IActionResult> PrintBill(int Id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sale = await db.sales.FirstOrDefaultAsync(s => s.SaleId == Id);
            if (sale == null)
                return NotFound("Sale record not found.");

            // ✅ Fetch Client name
            string customerName = "Walk-in Customer";
            if (sale.ClientId != null)
            {
                var client = await db.clients.FirstOrDefaultAsync(c => c.Clientid == sale.ClientId);
                if (client != null)
                    customerName = client.Name;
            }

            // ✅ Fetch Items related to this Sale
            var items = db.soldItems
                .Where(i => i.SaleId == Id)
                .Select(i => new
                {
                    ItemName = i.ItemName,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice
                })
                .ToList<dynamic>();

            // ✅ Calculate change
            double totalAmount = sale.Payment ?? 0;
            double cashReceived = sale.Cash_Received ?? 0;
            double changeBack = sale.Paid_Back ?? (cashReceived - totalAmount);

            string logoPath = Path.Combine(env.WebRootPath, "images", "logo.png");

            // ✅ Pass values into PDF Model
            var document = new InvoiceDocument
            {
                InvoiceNo = sale.SaleId.ToString(),
                DealingPerson = sale.DealingPerson,
                CustomerName = customerName,
                Serving = sale.Serving,
                TotalAmount = totalAmount,
                CashReceived = cashReceived,
                ChangeBack = changeBack,
                Items = items,
                LogoPath = System.IO.File.Exists(logoPath) ? logoPath : ""
            };

            // ✅ Generate PDF and Stream to Browser
            var pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Invoice_{sale.SaleId}.pdf");
        }

        public async Task<IActionResult> PrintPurchaseBill(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Include Supplier so navigation property is loaded
            var purchase = await db.Inv_Purchases
                                   .Include(p => p.Supplier)
                                   .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
                return NotFound("Purchase not found.");

            var items = await db.Inv_PurchasedItems
                                .Where(x => x.PurchaseId == id)
                                .ToListAsync();

            // Get absolute path for logo
            var logoPath = Path.Combine(env.WebRootPath, "assets", "img", "logo.png");

            var report = new PurchaseBillReport(purchase, items, logoPath);
            var pdfBytes = report.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"PurchaseBill_{purchase.InvoiceNo}.pdf");
        }



    }
}
