using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Client")]
    public class Client
    {
        [Key]
        public decimal Clientid { get; set; }
        public string? Name { get; set; }
    }
}
