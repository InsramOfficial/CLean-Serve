using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Inv_Items")]
    public class Item
    {
        [Key]
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public int? CategoryId { get; set; }
        public int? RecentUnitPrice { get; set; }
        public decimal? RetailPrice { get; set; }
        public int? Discount { get; set; }
        public string? Remarks { get; set; }
        public string? ItemType { get; set; }
        public string? Picture { get; set; }
        [NotMapped]
        public IFormFile ?image { get; set; }
    }
}
