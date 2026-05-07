using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System.Linq;

namespace SaigonRide.Controllers
{
    [Authorize] // Nhớ chặn không cho khách vào
    public class AdminStationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminStationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ĐỌC: Hiển thị danh sách Trạm
        public IActionResult Index(string searchString)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var stations = _context.Stations.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Trạm thì tìm theo Tên khu vực
                stations = stations.Where(s => s.LocationName.Contains(searchString));
            }

            ViewBag.CurrentSearch = searchString;
            return View(stations.ToList());
        }

        // TẠO MỚI: Hiển thị form thêm trạm
        [HttpGet]
        public IActionResult Create()
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");
            return View();
        }

        // TẠO MỚI: Xử lý dữ liệu khi Admin bấm "Lưu"
        [HttpPost]
        public IActionResult Create(Station station)
        {
            // Trạm mới xây thì số xe hiện tại bằng 0
            station.CurrentInventory = 0;

            _context.Stations.Add(station);
            _context.SaveChanges();

            TempData["Success"] = "Đã thêm trạm mới thành công!";
            return RedirectToAction("Index");
        }
        // SỬA: Hiển thị form cập nhật (kéo data cũ lên)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var station = _context.Stations.Find(id);
            if (station == null) return NotFound();

            return View(station);
        }

        // SỬA: Lưu thông tin sau khi Admin thay đổi
        [HttpPost]
        public IActionResult Edit(Station station)
        {
            _context.Stations.Update(station);
            _context.SaveChanges();

            TempData["Success"] = "Đã cập nhật thông tin trạm thành công!";
            return RedirectToAction("Index");
        }

        // XÓA: Gỡ trạm khỏi hệ thống (Cơ chế an toàn)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var station = _context.Stations.Find(id);
            if (station == null) return NotFound();

            // 1. Kiểm tra xe đang đỗ tại trạm này
            var hasVehicles = _context.Vehicles.Any(v => v.StationId == id);
            if (station.CurrentInventory > 0 || hasVehicles)
            {
                TempData["Error"] = "Lỗi: Không thể xóa! Trạm này vẫn đang có xe đỗ.";
                return RedirectToAction("Index");
            }

            // 2. THÊM MỚI: Kiểm tra lịch sử giao dịch
            // Nếu trạm đã từng nằm trong biên lai trả xe, cấm xóa!
            var hasTransactions = _context.RentalTransactions.Any(rt => rt.ReturnStationId == id);
            if (hasTransactions)
            {
                TempData["Error"] = "Lỗi: Không thể xóa! Trạm này đã có lịch sử giao dịch thuê/trả xe.";
                return RedirectToAction("Index");
            }

            // Vượt qua hết các chốt chặn thì mới được phép xóa
            _context.Stations.Remove(station);
            _context.SaveChanges();

            TempData["Success"] = "Đã xóa trạm thành công!";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var station = _context.Stations.Find(id);
            if (station != null)
            {
                station.IsAvailable = !station.IsAvailable;
                _context.SaveChanges();
                TempData["Success"] = "Đã thay đổi trạng thái hoạt động của trạm!";
            }
            return RedirectToAction("Index");
        }
    }
}