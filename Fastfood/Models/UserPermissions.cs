using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.Models
{
    [Table("UserPermission")]
	public class UserPermissions
	{
        [Key]
        public int? PermissionId { get; set; }
        public Guid UserCode { get; set; }
        public int? MethodId { get; set; }
        public string? MethodName { get; set; }
        public bool View { get; set; }
    }
}
