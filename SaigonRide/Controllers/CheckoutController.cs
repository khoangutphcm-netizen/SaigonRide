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
// 1. HÀM BẮT ĐẦU TÍNH GIỜ (Nâng cấp logic quản lý phiên)
        [HttpPost]
        public IActionResult StartTimer(int vehicleId, int returnStationId)
        {
            var vehicle = _context.Vehicles.FirstOrDefault(v => v.Id == vehicleId);
            var station = _context.Stations.FirstOrDefault(s => s.Id == returnStationId);

            if (vehicle == null || station == null) return NotFound();

            var startTime = DateTime.Now;
            var sessionKey = $"rental_{vehicleId}_{startTime.Ticks}";

            var rentalSession = new
            {
                VehicleId = vehicleId,
                ReturnStationId = returnStationId,
                StartTime = startTime.ToString("O"), // Ép chuẩn ISO 8601 theo chuẩn API
                AdditionalMinutes = 0
            };

            // Lưu thông tin vào Session để quản lý trạng thái thuê xe
            HttpContext.Session.SetString(sessionKey, System.Text.Json.JsonSerializer.Serialize(rentalSession));
            HttpContext.Session.SetString("current_rental_session", sessionKey);

            return RedirectToAction("Timer", new { vehicleId }); 
        }