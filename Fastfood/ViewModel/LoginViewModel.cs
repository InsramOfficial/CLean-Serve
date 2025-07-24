using System.ComponentModel.DataAnnotations;

namespace Fastfood.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter Your User ID")]
        public string ID { get; set; }
        [Required(ErrorMessage = "Enter Your Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
