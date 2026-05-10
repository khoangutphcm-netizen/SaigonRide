using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using System.Linq;
using System.Text.Json; // Bắt buộc phải có để đóng gói JSON

namespace SaigonRide.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Kéo Trạm và kèm theo danh sách Xe (Include)
            var stations = _context.Stations
                                   .Include(s => s.Vehicles)
                                   .Where(s => s.IsAvailable)
                                   .ToList();

            // 2. Đóng gói dữ liệu gửi xuống Javascript 
            var stationsData = stations.Select(s => new {
                id = s.Id,
                name = s.LocationName,
                lat = s.Latitude,
                lng = s.Longitude,
 
                // Tự động đếm chính xác số lượng xe đang ở trạng thái Available thực tế
                inventory = s.Vehicles.Count(v => v.Status == SaigonRide.Models.Enums.VehicleStatus.Available),
                
                capacity = s.MaxCapacity,

                vehicles = s.Vehicles.Where(v => v.Status == SaigonRide.Models.Enums.VehicleStatus.Available)
                                    .Select(v => new { id = v.Id, name = v.Category, price = v.PricePerMinute }).ToList()
            }).ToList();

            ViewBag.StationsJson = JsonSerializer.Serialize(stationsData);

            return View(stations);
        }
    }
}