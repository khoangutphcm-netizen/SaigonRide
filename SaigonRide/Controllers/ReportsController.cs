using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaigonRide.Controllers
{
    [Authorize] // Chỉ giữ cái này để yêu cầu đăng nhập chung
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Khách hàng gửi báo cáo (hỗ trợ hoặc liên quan chuyến đi)
        [HttpPost]
        public async Task<IActionResult> CreateReport(int rentalTransactionId, string title, string description)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            int userId = int.Parse(userIdString);

            // Nếu rentalTransactionId không phải 0, kiểm tra xem chuyến đi có tồn tại không
            if (rentalTransactionId != 0)
            {
                var transaction = await _context.RentalTransactions.FindAsync(rentalTransactionId);
                if (transaction == null)
                {
                    TempData["ErrorMessage"] = "Chuyến đi không tồn tại!";
                    return RedirectToAction("Index", "Home");
                }
            }

            var report = new Report
            {
                UserId = userId,
                RentalTransactionId = rentalTransactionId == 0 ? null : rentalTransactionId,
                Title = title,
                Description = description,
                Status = ReportStatus.Open,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi báo cáo thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Khách hàng xem báo cáo của mình
        public async Task<IActionResult> MyReports()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            int userId = int.Parse(userIdString);

            var reports = await _context.Reports
                .Include(r => r.RentalTransaction)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // Admin xem tất cả báo cáo (ĐÃ XÓA SẠCH [Authorize(Roles="Admin")])
        public async Task<IActionResult> AdminReports(string searchString)
        {
            if (User.FindFirst("UserType")?.Value != "Admin")
                return RedirectToAction("Index", "Home");

            var reports = _context.Reports
                .Include(r => r.User)
                .Include(r => r.RentalTransaction)
                .AsQueryable();
             
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                reports = reports.Where(r =>
                    r.Title.ToLower().Contains(searchString) ||
                    r.User.FullName.ToLower().Contains(searchString) ||
                    r.Id.ToString().Contains(searchString));
            }

            ViewBag.CurrentSearch = searchString;
            return View(await reports.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        // Admin trả lời báo cáo (ĐÃ XÓA SẠCH [Authorize(Roles="Admin")])
        [HttpPost]
        public async Task<IActionResult> RespondToReport(int reportId, string adminResponse, string status)
        {
            // Tự check quyền Admin thủ công
            if (User.FindFirst("UserType")?.Value != "Admin")
                return RedirectToAction("Index", "Home");

            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return NotFound();

            report.AdminResponse = adminResponse;
            report.Status = (ReportStatus)Enum.Parse(typeof(ReportStatus), status);
            report.RespondedAt = DateTime.Now;

            _context.Reports.Update(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi phản hồi thành công!";
            return RedirectToAction("AdminReports");
        }

        // Xem chi tiết báo cáo
        public async Task<IActionResult> ReportDetails(int id)
        {
            var report = await _context.Reports
                .Include(r => r.RentalTransaction)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.FindFirst("UserType")?.Value == "Admin";

            // Nếu không phải người tạo báo cáo và cũng không phải Admin thì đuổi ra
            if (report.UserId.ToString() != userIdString && !isAdmin)
                return RedirectToAction("Index", "Home");

            return View(report);
        }
    }
}