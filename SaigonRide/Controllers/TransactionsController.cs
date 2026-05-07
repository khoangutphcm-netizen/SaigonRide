using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using System.Linq;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string searchString)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var transactions = _context.RentalTransactions
                .Include(rt => rt.User)
                .Include(rt => rt.Vehicle)
                .AsQueryable();
             
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                transactions = transactions.Where(rt =>
                    rt.User.FullName.ToLower().Contains(searchString) ||
                    rt.Vehicle.Category.ToLower().Contains(searchString) ||
                    rt.Id.ToString().Contains(searchString));
            }

            ViewBag.CurrentSearch = searchString;
            return View(transactions.OrderByDescending(rt => rt.StartTime).ToList());
        }
    }
}