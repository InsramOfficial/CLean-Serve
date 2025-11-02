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
        // ✅ Generic Report Generator Method
        private async Task<byte[]> GenerateSalesReportAsync(DateTime startDate, DateTime endDate, string reportTitle)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sales = await db.sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .OrderBy(s => s.SaleDate)
                .ToListAsync();

            var logoPath = Path.Combine(env.WebRootPath, "images", "logo.png");
            if (!System.IO.File.Exists(logoPath))
                logoPath = "";

            var report = new GenericSalesReportDocument(sales, reportTitle, startDate, endDate, logoPath);
            return report.GeneratePdf();
        }

        // ✅ Daily Report
        public async Task<IActionResult> DailyReport()
        {
            var today = DateTime.Today;
            var pdf = await GenerateSalesReportAsync(today, today.AddDays(1), "Daily Sales Report");
            return File(pdf, "application/pdf", $"DailyReport_{today:yyyyMMdd}.pdf");
        }

        // ✅ Weekly Report
        public async Task<IActionResult> WeeklyReport()
        {
            var end = DateTime.Today;
            var start = end.AddDays(-7);
            var pdf = await GenerateSalesReportAsync(start, end, "Weekly Sales Report");
            return File(pdf, "application/pdf", $"WeeklyReport_{end:yyyyMMdd}.pdf");
        }

        // ✅ Monthly Report
        public async Task<IActionResult> MonthlyReport()
        {
            var now = DateTime.Today;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var pdf = await GenerateSalesReportAsync(start, end, "Monthly Sales Report");
            return File(pdf, "application/pdf", $"MonthlyReport_{now:yyyyMM}.pdf");
        }

        // ✅ Yearly Report
        public async Task<IActionResult> YearlyReport()
        {
            var now = DateTime.Today;
            var start = new DateTime(now.Year, 1, 1);
            var end = new DateTime(now.Year, 12, 31);
            var pdf = await GenerateSalesReportAsync(start, end, "Yearly Sales Report");
            return File(pdf, "application/pdf", $"YearlyReport_{now:yyyy}.pdf");
        }

        // ✅ Custom Range Report
        [HttpPost]
        public async Task<IActionResult> CustomReport(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                return BadRequest("Invalid date range");

            var pdf = await GenerateSalesReportAsync(startDate, endDate, $"Custom Report ({startDate:MMM dd} - {endDate:MMM dd})");
            return File(pdf, "application/pdf", $"CustomReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
        }


    }
}
