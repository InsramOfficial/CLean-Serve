namespace Fastfood.ViewModel
{
    public class PurchaseStockDashboardVM
    {
        public int TotalPurchases { get; set; }
        public int TotalSuppliers { get; set; }
        public decimal TotalItemsInStock { get; set; } 
        public string BestSellingItem { get; set; }
        public decimal BestSellingQty { get; set; }       
        public List<string> MonthlyLabels { get; set; }
        public List<decimal> MonthlyPurchaseValues { get; set; }  
        public List<string> BestSellingLabels { get; set; }
        public List<decimal> BestSellingValues { get; set; }     
        public List<RecentPurchaseVM> RecentPurchases { get; set; }
        public List<StockTrackingVM> LowStockItems { get; set; }

    }


    public class RecentPurchaseVM
    {
        public string InvoiceNo { get; set; }
        public string Supplier { get; set; }
        public string Date { get; set; }
        public string Item { get; set; }
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NetPrice { get; set; }
    }

}
