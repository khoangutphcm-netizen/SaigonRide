using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System;

namespace SaigonRide.Controllers
{
    public class LanguageController : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/",
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                cookieOptions
            );

            if (string.IsNullOrEmpty(returnUrl)) returnUrl = "/Home/Index";

            // KIỂM TRA ĐẶC BIỆT: Bảo toàn dữ liệu POST cho luồng Checkout (Rent, Receipt)
            if (Request.HasFormContentType && Request.Form.ContainsKey("vehicleId"))
            {
                // Dùng hàm chuyên dụng này để giữ nguyên method POST và tự động tạo Header chuyển hướng
                return LocalRedirectPreserveMethod(returnUrl);
            }

            // Với các trang bình thường (Home, Success, History), dùng Redirect thông thường
            return LocalRedirect(returnUrl);
        }
    }
}