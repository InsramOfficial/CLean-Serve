using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Methods")]
	public class Method
	{
        [Key]
        public int MethodId { get; set; }
        public string? MethodName { get; set; }
    }
}
