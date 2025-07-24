using Fastfood.Models;

namespace Fastfood.ViewModel
{
	public class AssignPermissionVM
	{
		public Register user { get; set; }
		public List<UserPermissions> permissions { get; set; } = new();
	}
}
