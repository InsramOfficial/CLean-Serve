using FastFood.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("UnitPrice")]
    public class UnitPrice
    {
        [Key]
        public int UnitId { get; set; }    // must be int
        public string? UnitName { get; set; }
        public string? UnitCode { get; set; }
        [ValidateNever]
        public virtual ICollection<StockTracking>? StockTrackings { get; set; }
        [ValidateNever]

        public virtual ICollection<Consumeable> Consumeables { get; set; }
    }

}
