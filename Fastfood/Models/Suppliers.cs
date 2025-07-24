using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("Supplier")]
    public class Suppliers
    {
        [Key]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Supplier name is required.")]
        [StringLength(500, ErrorMessage = "Name cannot exceed 500 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNo { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Mobile number cannot exceed 50 characters.")]
        public string? MobileNo { get; set; }

        [Required(ErrorMessage = "Creation date is required.")]
       
        public DateTime SupplierCreationDate { get; set; }


        [Required(ErrorMessage = "NIC is required.")]
        [StringLength(100, ErrorMessage = "NIC cannot exceed 100 characters.")]
        public string NIC { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = null!;

        [StringLength(50, ErrorMessage = "City code cannot exceed 50 characters.")]
        public string? Citycode { get; set; }

        [Required(ErrorMessage = "Country code is required.")]
        [StringLength(50, ErrorMessage = "Country code cannot exceed 50 characters.")]
        public string Countrycode { get; set; } = null!;

        public decimal? Accountid { get; set; }

        [Required]
        public bool DbStatus { get; set; } = true;

        public bool? ByDefault { get; set; }

        [StringLength(50)]
        public string? Modifier { get; set; }

      
        [StringLength(50, ErrorMessage = "Operation type cannot exceed 50 characters.")]
        public string? Operation_Type { get; set; }
    }
}
