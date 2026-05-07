using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using SaigonRide.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaigonRide.Middlewares
{
    public class BanCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public BanCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // DbContext phải được inject vào hàm InvokeAsync vì nó là Scoped Service
        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            // Chỉ kiểm tra những người đã đăng nhập
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdString, out int userId))
                {
                    // Truy vấn nhanh xem User này có đang bị khóa trong DB không
                    var user = await dbContext.Users.FindAsync(userId);

                    if (user != null && user.IsBanned)
                    {
                        // THỰC THI LỆNH INSTANT KICK OUT
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                        // Đá văng ra trang Login kèm theo thông báo
                        context.Response.Redirect("/Account/Login?error=banned");
                        return; // Chặn đứng luồng chạy, không cho đi tiếp vào Controller
                    }
                }
            }

            // Nếu không bị khóa, cho phép Request đi tiếp bình thường
            await _next(context);
        }
    }
}