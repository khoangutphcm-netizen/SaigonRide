using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System.Linq;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ĐỌC: Hiển thị danh sách người dùng 
        public IActionResult Index(string searchString)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            // Dùng AsQueryable để khoan hãy kéo data về vội, chờ ráp điều kiện lọc đã
            var users = _context.Users.AsQueryable();

            // Nếu Admin có gõ chữ vào thanh tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.FullName.Contains(searchString) ||
                                         u.Email.Contains(searchString) ||
                                         (u.Phone != null && u.Phone.Contains(searchString)));
            }

            // Lưu lại từ khóa để nhét ngược lại vào ô Input cho nó khỏi biến mất sau khi tìm
            ViewBag.CurrentSearch = searchString;

            return View(users.ToList());
        }
        // EDIT: Show user form
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var userInDb = _context.Users.Find(id);
            if (userInDb == null) return NotFound();

            return View(userInDb);
        }

        // EDIT: Save user changes
        [HttpPost] 
        public IActionResult Edit(User updatedUser)
        {
            // Tìm User thật sự trong Database ra
            var existingUser = _context.Users.Find(updatedUser.Id);
            if (existingUser != null)
            {
                // CHỈ CẬP NHẬT ĐÚNG 3 CỘT NÀY TỪ FORM ADMIN
                existingUser.FullName = updatedUser.FullName;
                existingUser.Phone = updatedUser.Phone;
                existingUser.DocumentId = updatedUser.DocumentId;

                // TUYỆT ĐỐI KHÔNG ĐỤNG VÀO (Wallet, Password, CreatedAt, v.v...)

                _context.SaveChanges();
                TempData["Success"] = "Đã cập nhật thông tin khách hàng!";
            }
            return RedirectToAction("Index");
        }
        // HÀNH ĐỘNG: Khóa / Mở khóa tài khoản
        [HttpPost]
        public IActionResult ToggleBan(int id)
        {
            var userInDb = _context.Users.Find(id);
            if (userInDb != null)
            {
                // Bảo vệ tài khoản Admin gốc (tránh tự hủy)
                if (userInDb.Email == "admin@saigonride.com")
                {
                    TempData["Error"] = "Lỗi: Không thể khóa tài khoản System Admin!";
                    return RedirectToAction("Index");
                }

                // Lật ngược trạng thái: Đang khóa -> Mở, Đang mở -> Khóa
                userInDb.IsBanned = !userInDb.IsBanned;
                _context.SaveChanges();

                TempData["Success"] = userInDb.IsBanned
                    ? $"Đã KHÓA tài khoản của {userInDb.FullName}."
                    : $"Đã MỞ KHÓA tài khoản của {userInDb.FullName}.";
            }
            return RedirectToAction("Index");
        }
    }
}