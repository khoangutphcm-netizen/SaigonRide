using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using SaigonRide.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.Json;

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

        public IActionResult Rent(int vehicleId)
        {
            var vehicle = _context.Vehicles.Include(v => v.Station).FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null) return NotFound();

            var stationsData = _context.Stations.Select(s => new {
                id = s.Id,
                name = s.LocationName,
                lat = s.Latitude,
                lng = s.Longitude,
                inventory = s.CurrentInventory,
                capacity = s.MaxCapacity
            }).ToList();

            ViewBag.StationsJson = System.Text.Json.JsonSerializer.Serialize(stationsData);

            return View(vehicle);
        }

        [HttpGet]
        public IActionResult Receipt(int vehicleId, int returnStationId, int duration)
        {
            decimal finalPrice = _rentalService.CalculateEstimatedFare(vehicleId, returnStationId, duration);
            var vehicle = _context.Vehicles.FirstOrDefault(v => v.Id == vehicleId);
            var station = _context.Stations.FirstOrDefault(s => s.Id == returnStationId);

            ViewBag.VehicleId = vehicleId;
            ViewBag.ReturnStationId = returnStationId;
            ViewBag.VehicleName = vehicle?.Category;
            ViewBag.StationName = station?.LocationName;
            ViewBag.Duration = duration;
            ViewBag.BasePrice = duration * (vehicle?.PricePerMinute ?? 0);
            ViewBag.FinalPrice = finalPrice;
            ViewBag.IsDiscounted = finalPrice < ViewBag.BasePrice;

            return View();
        }

        // ĐÃ NÂNG CẤP: Bổ sung tham số paymentMethod
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int vehicleId, int returnStationId, int duration, string paymentMethod)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null) return Content($"LỖI: Không tìm thấy xe có ID = {vehicleId}.");

            var returnStation = await _context.Stations.FindAsync(returnStationId);
            if (returnStation == null) return Content($"LỖI: Không tìm thấy trạm trả có ID = {returnStationId}.");

            decimal finalPrice = _rentalService.CalculateEstimatedFare(vehicleId, returnStationId, duration);

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdString);

            // ==========================================
            // LOGIC RẼ NHÁNH THANH TOÁN
            // ==========================================

            // NẾU LÀ THANH TOÁN ONLINE -> Chuyển hướng sang Cổng giả lập
            if (paymentMethod != "cash")
            {
                var pendingOrder = new PendingRentalOrder
                {
                    VehicleId = vehicleId,
                    ReturnStationId = returnStationId,
                    Duration = duration,
                    FinalPrice = finalPrice,
                    CurrentUserId = currentUserId,
                    CreatedAt = DateTime.Now,
                    VehicleName = vehicle.Category,
                    ReturnStationName = returnStation.LocationName,
                    BasePrice = vehicle.PricePerMinute * duration,
                    IsDiscountApplied = finalPrice < (vehicle.PricePerMinute * duration),
                    PaymentMethod = paymentMethod // Lưu lại để View giả lập hiển thị đúng Logo
                };

                TempData["PendingOrder"] = JsonSerializer.Serialize(pendingOrder);
                return RedirectToAction("ShowPaymentForm", "MockPaymentGateway");
            }

            // NẾU LÀ THANH TOÁN TIỀN MẶT (CASH) -> Lưu Database luôn, bỏ qua cổng giả lập
            try
            {
                var transaction = new RentalTransaction
                {
                    UserId = currentUserId,
                    VehicleId = vehicleId,
                    ReturnStationId = returnStationId,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddMinutes(duration),
                    TotalDue = finalPrice,
                    IsDiscountApplied = finalPrice < (vehicle.PricePerMinute * duration)
                };

                _context.RentalTransactions.Add(transaction);

                int oldStationId = vehicle.StationId;
                vehicle.StationId = returnStationId;
                vehicle.Status = Models.Enums.VehicleStatus.Available;

                if (oldStationId != returnStationId)
                {
                    var oldStation = await _context.Stations.FindAsync(oldStationId);
                    if (oldStation != null)
                    {
                        oldStation.CurrentInventory = await _context.Vehicles.CountAsync(v => v.StationId == oldStationId);
                    }
                }

                returnStation.CurrentInventory = await _context.Vehicles.CountAsync(v => v.StationId == returnStationId);

                await _context.SaveChangesAsync();
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                return Content($"LỖI HỆ THỐNG KHI LƯU DB: {ex.Message}");
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        // 1. HÀM BẮT ĐẦU TÍNH GIỜ
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
                StartTime = startTime.ToString("O"), // Ép chuẩn ISO 8601
                AdditionalMinutes = 0
            };

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(rentalSession));
            HttpContext.Session.SetString("current_rental_session", sessionKey);

            return RedirectToAction("Timer", new { vehicleId }); // Chuyển hướng cho chuẩn MVC
        }

        // 2. HÀM HIỂN THỊ GIAO DIỆN TIMER (Hàm bị thiếu gây lỗi 404)
        [HttpGet]
        public IActionResult Timer(int vehicleId)
        {
            var sessionKey = HttpContext.Session.GetString("current_rental_session");
            if (string.IsNullOrEmpty(sessionKey)) return RedirectToAction("Index", "Booking");

            var sessionData = HttpContext.Session.GetString(sessionKey);
            using (JsonDocument doc = JsonDocument.Parse(sessionData))
            {
                var root = doc.RootElement;
                int returnStationId = root.GetProperty("ReturnStationId").GetInt32();

                var vehicle = _context.Vehicles.FirstOrDefault(v => v.Id == vehicleId);
                var station = _context.Stations.FirstOrDefault(s => s.Id == returnStationId);

                ViewBag.VehicleId = vehicleId;
                ViewBag.VehicleCategory = vehicle?.Category;
                ViewBag.StationName = station?.LocationName;
                ViewBag.PricePerMinute = vehicle?.PricePerMinute ?? 0;

                // Trị lỗi NaN: Ép thời gian thành số nguyên Unix (Miliseconds)
                DateTime st = DateTime.Parse(root.GetProperty("StartTime").GetString());
                ViewBag.StartTimeUnix = ((DateTimeOffset)st).ToUnixTimeMilliseconds();

                ViewBag.AdditionalMinutes = root.GetProperty("AdditionalMinutes").GetInt32();
            }

            return View();
        }

        // 3. HÀM CỘNG THÊM PHÚT (Đã fix lỗi JSON)
        [HttpPost]
        public IActionResult AddMinutes(int vehicleId, int minutes)
        {
            var sessionKey = HttpContext.Session.GetString("current_rental_session");
            if (string.IsNullOrEmpty(sessionKey)) return RedirectToAction("Index", "Booking");

            var sessionData = HttpContext.Session.GetString(sessionKey);
            using (JsonDocument doc = JsonDocument.Parse(sessionData))
            {
                var root = doc.RootElement;
                var updatedSession = new
                {
                    VehicleId = vehicleId,
                    ReturnStationId = root.GetProperty("ReturnStationId").GetInt32(),
                    StartTime = root.GetProperty("StartTime").GetString(),
                    AdditionalMinutes = root.GetProperty("AdditionalMinutes").GetInt32() + minutes
                };
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(updatedSession));
            }

            return RedirectToAction("Timer", new { vehicleId });
        }

        // 4. HÀM KẾT THÚC CHUYẾN ĐI (Đã fix lỗi JSON)
        [HttpPost]
        public async Task<IActionResult> EndRental(int vehicleId)
        {
            var sessionKey = HttpContext.Session.GetString("current_rental_session");
            if (string.IsNullOrEmpty(sessionKey)) return RedirectToAction("Index", "Booking");

            var sessionData = HttpContext.Session.GetString(sessionKey);

            int returnStationId;
            DateTime startTime;
            int additionalMinutes;

            using (JsonDocument doc = JsonDocument.Parse(sessionData))
            {
                var root = doc.RootElement;
                returnStationId = root.GetProperty("ReturnStationId").GetInt32();
                startTime = DateTime.Parse(root.GetProperty("StartTime").GetString());
                additionalMinutes = root.GetProperty("AdditionalMinutes").GetInt32();
            }

            var elapsed = DateTime.Now - startTime;
            int actualMinutes = (int)Math.Ceiling(elapsed.TotalMinutes) + additionalMinutes;

            // Xóa session chuyến đi
            HttpContext.Session.Remove(sessionKey);
            HttpContext.Session.Remove("current_rental_session");

            // ĐÁ KHÁCH SANG TRANG RECEIPT ĐỂ CHỌN 6 PHƯƠNG THỨC THANH TOÁN
            return RedirectToAction("Receipt", new { vehicleId = vehicleId, returnStationId = returnStationId, duration = actualMinutes });
        }
    }
}