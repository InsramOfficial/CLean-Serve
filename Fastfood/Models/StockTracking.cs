using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("StockTracking")]
    public class StockTracking
    {
        [Key]
        public int StockID { get; set; }

        public int? TrsID { get; set; }

        public DateTime? TrsDate { get; set; } 
        public int ItemId { get; set; }

        public decimal? Qty { get; set; }

        public string? Source { get; set; }

        public decimal? Price { get; set; }

        public string? ItemName { get; set; }
        // NEW: UnitId FK to UnitPrice
        public int? UnitId { get; set; }

        [ForeignKey("UnitId")]
        public virtual UnitPrice? UnitPrice { get; set; }
        public Item Item { get; set; }  
    }
}
