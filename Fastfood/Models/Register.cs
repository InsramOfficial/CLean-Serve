using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Logins")]
    public class Register
    {
        [Key]
        public Guid UserCode { get; set; }
        public string? ID { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public string? Access { get; set; }
    }
}
