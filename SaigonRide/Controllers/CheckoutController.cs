using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using SaigonRide.Services;
using System.Threading.Tasks;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RentalService _rentalService;

        public CheckoutController(ApplicationDbContext context, RentalService rentalService)
        {
            _context = context;
            _rentalService = rentalService;
        }

        // Bước khởi tạo Use Case: Điều hướng đến trang Rent
        public IActionResult Rent(int vehicleId)
        {
            var vehicle = _context.Vehicles.Include(v => v.Station).FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null) return NotFound();
            return View(vehicle);
        }
    }
}