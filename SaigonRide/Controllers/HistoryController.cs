using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string type = "all", string period = "all")
        {
            // Lọc bằng UserId thay vì Email
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdString);

            var query = _context.RentalTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ReturnStation)
                .Where(t => t.UserId == currentUserId);

            if (type == "bike")
            {
                query = query.Where(t => !t.Vehicle.Category.Contains("Scooter"));
            }
            else if (type == "scooter")
            {
                query = query.Where(t => t.Vehicle.Category.Contains("Scooter"));
            }

            if (period == "month")
            {
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                query = query.Where(t => t.StartTime >= startOfMonth);
            }
            else if (period == "year")
            {
                var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
                query = query.Where(t => t.StartTime >= startOfYear);
            }

            var rides = await query.OrderByDescending(t => t.StartTime).ToListAsync();

            ViewBag.ActiveType = type;
            ViewBag.ActivePeriod = period;

            return View(rides);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdString);

            var transaction = await _context.RentalTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ReturnStation)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

            if (transaction == null) return NotFound();

            return View(transaction);
        }
    }
}