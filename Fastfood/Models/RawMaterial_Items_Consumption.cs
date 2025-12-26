using FastFood.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("RawMaterial_Items_Consumption")]
    public class RawMaterial_Items_Consumption
    {
        [Key]
        
        public int S_No { get; set; }

        
        public int? RMInv_ItemId { get; set; }

         
        public string? RMItemName { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal RMQTY { get; set; }


        public int? BInv_ItemId { get; set; }
        public int? UnitId { get; set; }  // FK to UnitPrice table
       [ValidateNever] 
        public UnitPrice Unit { get; set; }

        [ForeignKey("BInv_ItemId")]
        public Item? Item { get; set; }
        public string? Remarks { get; set; }
        [NotMapped]
        public bool IsDeleted { get; set; }
    }
}
