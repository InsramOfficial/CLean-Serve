using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Inv_SaledItems")]
    public class SoldItems
    {
        [Key]
        public int EntryId { get; set; }
        public int SaleId { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public int Qty { get; set; }
        public int? UnitPrice { get; set; }
        public int? Discount { get; set; }
        //public string? Saletype { get; set; }
        public string? NetPrice { get; set; }
        //public string? Packing_cost { get; set; }
    }
}
