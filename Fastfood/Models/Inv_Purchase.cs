using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Inv_Purchase")]
    public class Inv_Purchase
    {
        [Key]
        public int PurchaseId { get; set; }

        public int SupplierId { get; set; }

        public DateTime? PurchaseDate { get; set; }
        public string? DealingPerson { get; set; }
        public string? OrderOrpurchase { get; set; }
        public decimal? InvoiceNo { get; set; }

        public decimal? Payment { get; set; }

        public decimal? FlatDisc { get; set; }

        public decimal? Misc { get; set; }

        public decimal? Commesion { get; set; }

        // Navigation Properties
        public Suppliers? Supplier { get; set; }

        public List<Inv_PurchasedItems>? PurchasedItems { get; set; }
    }
}
