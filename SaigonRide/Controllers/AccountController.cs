using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SaigonRide.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email này đã được sử dụng!";
                return View();
            }

            user.AvatarUrl ??= "/images/avatars/default.png";
            user.UserType ??= "Tourist"; // Chốt chặn: User đăng ký mới luôn là dân thường

            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng!";
                return View();
            }
            if (user.IsBanned)
            {
                ViewBag.Error = "Tài Khoản của bạn đã bị khóa!\nYour Account is banned";
                return View();
            }
            var claims = new List<Claim>
            {
                // 🔥 ĐÂY LÀ DÒNG CHỐT HẠ ĐÃ CỨU SỐNG TOÀN BỘ HỆ THỐNG 🔥
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserType", user.UserType ?? "Tourist"),
                new Claim("DocumentId", user.DocumentId ?? ""),
                new Claim("AvatarUrl", user.AvatarUrl ?? "/images/avatars/default.png"),
                new Claim(ClaimTypes.MobilePhone, user.Phone ?? "+84 987 654 321")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Điều hướng thẳng vào AdminDashboard cho chuẩn
            if (user.UserType == "Admin") return RedirectToAction("Index", "AdminDashboard");
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Intro");
        }
    }
}