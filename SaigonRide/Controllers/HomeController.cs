using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using System.Linq;
using System.Text.Json;
using System.Security.Claims; // BẮT BUỘC THÊM CÁI NÀY ĐỂ LẤY USER ID

namespace SaigonRide.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang Dashboard chính của Khách
        public IActionResult Index()
        {
            // 1. LẤY ID CỦA USER ĐANG ĐĂNG NHẬP
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(userIdString);

            // 2. LẤY LỊCH SỬ CHUYẾN ĐI (CHỈ CỦA USER NÀY)
            var recentRides = _context.RentalTransactions
                                      .Include(rt => rt.Vehicle)
                                      .Where(rt => rt.UserId == currentUserId) // ĐÃ FIX LỖI: Lọc theo đúng ID
                                      .OrderByDescending(rt => rt.StartTime)
                                      .Take(5)
                                      .ToList();

            // 3. TÍNH TOÁN THỐNG KÊ CHI TIÊU (CHỈ CỦA USER NÀY)
            var allUserRides = _context.RentalTransactions
                                       .Where(rt => rt.UserId == currentUserId) // ĐÃ FIX LỖI: Lọc theo đúng ID
                                       .ToList();

            ViewBag.TotalRides = allUserRides.Count;
            ViewBag.TotalSpending = allUserRides.Any() ? allUserRides.Sum(r => r.TotalDue) : 0;

            // Giả định mỗi chuyến trung bình 25 phút
            ViewBag.TotalMinutes = allUserRides.Count * 25;
            ViewBag.AvgCost = allUserRides.Any() ? allUserRides.Average(r => r.TotalDue) : 0;

            // DỮ LIỆU BIỂU ĐỒ (Giả lập để biểu đồ vẽ đẹp)
            var chartLabels = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            var chartData = new[] { 15000, 25000, 10000, 45000, 20000, 55000, 30000 };
            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(chartData);

            // KÉO THÔNG BÁO TỪ DATABASE RA (Lấy 3 thông báo mới nhất)
            ViewBag.Announcements = _context.Announcements
                                            .Where(a => a.IsActive)
                                            .OrderByDescending(a => a.CreatedAt)
                                            .Take(3)
                                            .ToList();

            return View(recentRides);
        }

        // Logic cho trang Xem tất cả thông báo
        public IActionResult Announcements()
        {
            var list = _context.Announcements
                               .Where(a => a.IsActive)
                               .OrderByDescending(a => a.CreatedAt)
                               .ToList();
            return View(list);
        }
    }
}