using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("BankSattlement")]
    public class BankSattlement
    {
        [Key]
        public int BSID { get; set; }
        public string BankName { get; set; }
        public int? BIN { get; set; }
        public string Card_Cat { get; set; }
        public string CardType { get; set; }
        public int? DiscountPercent { get; set; }
    }
}
