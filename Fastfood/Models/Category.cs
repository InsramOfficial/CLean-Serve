using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Category")]
    public class Category
    {
        [Key]
        public int? CategoryId { get; set; }
        [Required(ErrorMessage = "Category name is required.")]
        public string? CategoryName { get; set; }
        public int? Root {  get; set; }
        
    }
}
