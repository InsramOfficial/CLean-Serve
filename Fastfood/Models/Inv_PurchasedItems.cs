using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Inv_PurchasedItems")]
    public class Inv_PurchasedItems
    {
        [Key]
        public int EntryId { get; set; }       

        public int PurchaseId { get; set; }    
        public int ItemId { get; set; }      
        public string? ItemName { get; set; }
        public decimal Qty { get; set; }        
        public decimal UnitPrice { get; set; } 
        public decimal NetPrice { get; set; } 
        public string PurchaseType { get; set; }

        // Navigation properties
        public Inv_Purchase Purchase { get; set; }
        public Item Item { get; set; }

    }
}
