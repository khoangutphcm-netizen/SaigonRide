using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using SaigonRide.Models.Enums;
using System.Linq;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class AdminVehicleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminVehicleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ĐỌC: Danh sách xe
        public IActionResult Index(string searchString)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var vehicles = _context.Vehicles.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Xe thì tìm theo Loại xe
                vehicles = vehicles.Where(v => v.Category.Contains(searchString));
            }

            ViewBag.CurrentSearch = searchString;
            ViewBag.Stations = _context.Stations.ToDictionary(s => s.Id, s => s.LocationName);

            return View(vehicles.ToList());
        }

        // TẠO: Form thêm xe
        [HttpGet]
        public IActionResult Create()
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            // Đổ danh sách trạm vào Dropdown
            ViewBag.Stations = new SelectList(_context.Stations, "Id", "LocationName");
            return View();
        }

        // TẠO: Xử lý lưu xe & Tự động cộng xe cho trạm
        [HttpPost]
        public IActionResult Create(Vehicle vehicle)
        {
            // Xe mới mặc định là Available
            vehicle.Status = VehicleStatus.Available;
            _context.Vehicles.Add(vehicle);

            // LOGIC ĂN ĐIỂM: Cộng 1 vào CurrentInventory của Trạm
            var station = _context.Stations.Find(vehicle.StationId);
            if (station != null) { station.CurrentInventory += 1; }

            _context.SaveChanges();
            TempData["Success"] = "Đã thêm xe mới thành công!";
            return RedirectToAction("Index");
        }

        // ĐỔI TRẠNG THÁI NHANH (Sẵn sàng <-> Bảo trì)
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var vehicle = _context.Vehicles.Find(id);
            if (vehicle != null)
            {
                vehicle.Status = vehicle.Status == VehicleStatus.Available ? VehicleStatus.UnderMaintenance : VehicleStatus.Available;
                _context.SaveChanges();
                TempData["Success"] = "Đã thay đổi trạng thái xe!";
            }
            return RedirectToAction("Index");
        }
        // EDIT: Show the form with existing data
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var vehicle = _context.Vehicles.Find(id);
            if (vehicle == null) return NotFound();

            // Pass the station list for the dropdown
            ViewBag.Stations = new SelectList(_context.Stations, "Id", "LocationName", vehicle.StationId);
            return View(vehicle);
        }

        // EDIT: Save the changes
        [HttpPost]
        public IActionResult Edit(Vehicle vehicle)
        {
            var originalVehicle = _context.Vehicles.AsNoTracking().FirstOrDefault(v => v.Id == vehicle.Id);

            if (originalVehicle != null && originalVehicle.StationId != vehicle.StationId)
            {
                // If the station changed, update the inventory of both stations
                var oldStation = _context.Stations.Find(originalVehicle.StationId);
                if (oldStation != null && oldStation.CurrentInventory > 0) oldStation.CurrentInventory--;

                var newStation = _context.Stations.Find(vehicle.StationId);
                if (newStation != null) newStation.CurrentInventory++;
            }

            _context.Vehicles.Update(vehicle);
            _context.SaveChanges();

            TempData["Success"] = "Đã cập nhật thông tin xe thành công!";
            return RedirectToAction("Index");
        }
        // XÓA XE & Trừ xe khỏi trạm
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var vehicle = _context.Vehicles.Find(id);
            if (vehicle != null)
            {
                // LOGIC ĂN ĐIỂM: Trừ 1 vào CurrentInventory của Trạm
                var station = _context.Stations.Find(vehicle.StationId);
                if (station != null && station.CurrentInventory > 0) { station.CurrentInventory -= 1; }

                _context.Vehicles.Remove(vehicle);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa xe khỏi hệ thống!";
            }
            return RedirectToAction("Index");
        }
    }
}