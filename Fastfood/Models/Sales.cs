using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Inv_Sale")]
    public class Sales
    {
        [Key]
        public int SaleId { get; set; }
        public DateTime? SaleDate { get; set; }
        public DateTime? LastModified { get; set; }
        public double? Payment { get; set; }
        public string? Status { get; set; }
        public double? Cash_Received { get; set; }
        public double? Paid_Back { get; set; }
        public string? Modifier { get; set; }
        public int? TokenNumber { get; set; }
        public string? Serving { get; set; }
    }
}