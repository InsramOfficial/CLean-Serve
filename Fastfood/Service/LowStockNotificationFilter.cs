using Fastfood.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class LowStockNotificationFilter : IAsyncActionFilter
{
    private readonly DataDbContext _db;

    public LowStockNotificationFilter(DataDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        
        var stockReport = await _db.StockTracking
            .GroupBy(st => new { st.ItemId, st.ItemName })
            .Select(g => new
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName ?? "Unknown Item",
                Purchased = g.Where(x => x.Source == "Purchase")
                             .Sum(x => (decimal?)x.Qty) ?? 0m,
                Sold = g.Where(x => x.Source == "Sale")
                        .Sum(x => (decimal?)(x.Qty < 0 ? -x.Qty : x.Qty)) ?? 0m
            })
            .Select(x => new
            {
                x.ItemId,
                x.ItemName,
                Remaining = x.Purchased - x.Sold
            })
            .Where(x => x.Remaining <= 10)
            .OrderBy(x => x.Remaining)
            .ToListAsync();

        var lowStockNotifications = stockReport.Select(x => new
        {
            x.ItemName,
            x.Remaining,
            DetectedAt = DateTime.Now
        }).ToList();

        if (context.Controller is Controller controller)
        {
            controller.ViewBag.LowStockNotifications = lowStockNotifications;
            controller.ViewBag.NotificationCount = lowStockNotifications.Count;
        }

        await next();
    }
}
