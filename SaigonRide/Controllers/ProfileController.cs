using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Localization;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System.Linq;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IStringLocalizer<ProfileController> _localizer;
        private readonly ApplicationDbContext _context;

        public ProfileController(IStringLocalizer<ProfileController> localizer, ApplicationDbContext context)
        {
            _localizer = localizer;
            _context = context;
        }

        public IActionResult Index()
        {
            var userName = User.Identity.Name;
            var userInDb = _context.Users.FirstOrDefault(u => u.FullName == userName);

            if (userInDb != null)
            {
                ViewBag.CurrentName = userInDb.FullName;
                ViewBag.CurrentEmail = userInDb.Email;
                ViewBag.CurrentPhone = userInDb.Phone ?? "+84 987 654 321";
                ViewBag.AvatarPath = userInDb.AvatarUrl ?? "/images/avatars/default.png";
            }
            else
            {
                ViewBag.CurrentName = userName;
                ViewBag.CurrentEmail = "user@saigonride.vn";
                ViewBag.CurrentPhone = "+84 987 654 321";
                ViewBag.AvatarPath = "/images/avatars/default.png";
            }

            return View();
        }

        public IActionResult Support()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string phone)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                TempData["ErrorMessage"] = _localizer["MsgUpdateError"].Value;
                return RedirectToAction("Index", new { culture = System.Globalization.CultureInfo.CurrentUICulture.Name });
            }

            var identity = (ClaimsIdentity)User.Identity;
            string oldName = identity.Name;

            var userInDb = _context.Users.FirstOrDefault(u => u.FullName == oldName);
            if (userInDb == null)
            {
                userInDb = new User
                {
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    UserType = User.FindFirst("UserType")?.Value ?? "Tourist",
                    Password = "123",
                    DocumentId = "000000000"
                };
                _context.Users.Add(userInDb);
            }
            else
            {
                userInDb.FullName = fullName;
                userInDb.Email = email;
                userInDb.Phone = phone;
            }

            // Đã dọn sạch vòng lặp sửa tên cũ vì giờ mọi thứ dính liền với UserId

            await _context.SaveChangesAsync();

            var claimName = identity.FindFirst(ClaimTypes.Name);
            if (claimName != null) { identity.RemoveClaim(claimName); }
            identity.AddClaim(new Claim(ClaimTypes.Name, fullName));

            var claimAvatar = identity.FindFirst("AvatarUrl");
            if (claimAvatar != null) { identity.RemoveClaim(claimAvatar); }
            identity.AddClaim(new Claim("AvatarUrl", userInDb.AvatarUrl ?? "/images/avatars/default.png"));

            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            TempData["SuccessMessage"] = _localizer["MsgUpdateSuccess"].Value;
            return RedirectToAction("Index", new { culture = System.Globalization.CultureInfo.CurrentUICulture.Name });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                if (!Directory.Exists(uploadsFolder)) { Directory.CreateDirectory(uploadsFolder); }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + avatarFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                var avatarUrl = "/images/avatars/" + uniqueFileName;

                var userInDb = _context.Users.FirstOrDefault(u => u.FullName == User.Identity.Name);
                if (userInDb != null)
                {
                    userInDb.AvatarUrl = avatarUrl;
                    await _context.SaveChangesAsync();
                }

                var identity = (ClaimsIdentity)User.Identity;
                var claimAvatar = identity.FindFirst("AvatarUrl");
                if (claimAvatar != null) { identity.RemoveClaim(claimAvatar); }
                identity.AddClaim(new Claim("AvatarUrl", avatarUrl));

                var principal = new ClaimsPrincipal(identity);
                var authProperties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                TempData["SuccessMessage"] = _localizer["MsgAvatarSuccess"].Value;
            }
            return RedirectToAction("Index", new { culture = System.Globalization.CultureInfo.CurrentUICulture.Name });
        }
    }
}