    namespace Fastfood.ViewModel
    {
        public class StockTrackingVM
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public decimal Purchased { get; set; }
            public decimal Sold { get; set; }
        public decimal Consumed { get; set; }
        public decimal Remaining { get; set; }
        }

    }
