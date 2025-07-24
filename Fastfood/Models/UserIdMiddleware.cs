using System.Security.Claims;

namespace Fastfood.Models
{
	public class UserIdMiddleware
	{
		private readonly RequestDelegate _next;

		public UserIdMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			var userCode = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			// You can add more user-related information here...

			context.Items["UserCode"] = userCode;

			await _next(context);
		}
	}
}
