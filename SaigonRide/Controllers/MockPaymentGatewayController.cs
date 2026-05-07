using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class MockPaymentGatewayController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MockPaymentGatewayController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// BƯỚC 3: Hiển thị giao diện giả lập cổng thanh toán
        /// Màn hình này trông giống VNPay/MoMo, có 2 nút:
        /// - Xanh: Thanh toán thành công
        /// - Xám: Hủy giao dịch
        /// </summary>
        [HttpGet]
        public IActionResult ShowPaymentForm()
        {
            // Lấy dữ liệu đơn hàng từ TempData
            var pendingOrderJson = TempData["PendingOrder"] as string;
            if (string.IsNullOrEmpty(pendingOrderJson))
            {
                TempData["Error"] = "Lỗi: Không tìm thấy thông tin đơn hàng. Vui lòng thử lại!";
                return RedirectToAction("Index", "Booking");
            }

            // Giải mã JSON trở lại object
            var pendingOrder = JsonSerializer.Deserialize<PendingRentalOrder>(pendingOrderJson);
            if (pendingOrder == null)
            {
                TempData["Error"] = "Lỗi: Dữ liệu đơn hàng không hợp lệ.";
                return RedirectToAction("Index", "Booking");
            }

            // Lưu lại vào TempData để dùng khi callback
            TempData["PendingOrder"] = pendingOrderJson;

            return View(pendingOrder);
        }

        /// <summary>
        /// BƯỚC 4A: Callback khi thanh toán THÀNH CÔNG
        /// Đây là lúc mở khóa DATABASE và lưu giao dịch vào RentalTransactions
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PaymentSuccess()
        {
            // Lấy dữ liệu đơn hàng từ TempData
            var pendingOrderJson = TempData["PendingOrder"] as string;
            if (string.IsNullOrEmpty(pendingOrderJson))
            {
                TempData["Error"] = "Lỗi: Phiên thanh toán hết hạn. Vui lòng thử lại!";
                return RedirectToAction("Index", "Booking");
            }

            var pendingOrder = JsonSerializer.Deserialize<PendingRentalOrder>(pendingOrderJson);
            if (pendingOrder == null)
            {
                TempData["Error"] = "Lỗi: Không thể xử lý thanh toán.";
                return RedirectToAction("Index", "Booking");
            }

            try
            {
                // ========================================
                // MỞ KHÓA DATABASE - LƯU GIAO DỊCH
                // ========================================

                // 1. Lấy thông tin xe
                var vehicle = await _context.Vehicles.FindAsync(pendingOrder.VehicleId);
                if (vehicle == null)
                    throw new Exception("Xe không tồn tại trong hệ thống.");

                // 2. Lấy thông tin trạm đích
                var returnStation = await _context.Stations.FindAsync(pendingOrder.ReturnStationId);
                if (returnStation == null)
                    throw new Exception("Trạm trả xe không tồn tại trong hệ thống.");

                // 3. Tạo giao dịch chính thức
                var transaction = new RentalTransaction
                {
                    UserId = pendingOrder.CurrentUserId,
                    VehicleId = pendingOrder.VehicleId,
                    ReturnStationId = pendingOrder.ReturnStationId,
                    StartTime = pendingOrder.CreatedAt,
                    EndTime = pendingOrder.CreatedAt.AddMinutes(pendingOrder.Duration),
                    TotalDue = pendingOrder.FinalPrice,
                    IsDiscountApplied = pendingOrder.IsDiscountApplied
                };

                _context.RentalTransactions.Add(transaction);

                // 4. Cập nhật vị trí xe (Di chuyển sang trạm mới)
                int oldStationId = vehicle.StationId;
                vehicle.StationId = pendingOrder.ReturnStationId;
                vehicle.Status = Models.Enums.VehicleStatus.Available;

                _context.Vehicles.Update(vehicle);

                // 5. Cập nhật tồn kho trạm cũ (nếu xe di chuyển sang trạm khác)
                if (oldStationId != pendingOrder.ReturnStationId)
                {
                    var oldStation = await _context.Stations.FindAsync(oldStationId);
                    if (oldStation != null)
                    {
                        oldStation.CurrentInventory = await _context.Vehicles
                            .CountAsync(v => v.StationId == oldStationId && v.Status == Models.Enums.VehicleStatus.Available);
                        _context.Stations.Update(oldStation);
                    }
                }

                // 6. Cập nhật tồn kho trạm đích
                returnStation.CurrentInventory = await _context.Vehicles
                    .CountAsync(v => v.StationId == pendingOrder.ReturnStationId && v.Status == Models.Enums.VehicleStatus.Available);
                _context.Stations.Update(returnStation);

                // 7. LƯU TẤT CẢ VÀO DATABASE
                await _context.SaveChangesAsync();

                // 8. Xóa TempData sau khi lưu thành công
                TempData.Remove("PendingOrder");

                // 9. Chuyển hướng sang trang Success
                return RedirectToAction("Success", "Checkout");
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, xóa đơn hàng nháp và thông báo
                TempData.Remove("PendingOrder");
                TempData["Error"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("Index", "Booking");
            }
        }

        /// <summary>
        /// BƯỚC 4B: Callback khi hủy giao dịch
        /// Xóa đơn hàng nháp và thông báo lỗi
        /// </summary>
        [HttpPost]
        public IActionResult PaymentCancelled()
        {
            // Xóa đơn hàng nháp khỏi TempData
            TempData.Remove("PendingOrder");

            // Thông báo lỗi cho người dùng
            TempData["Error"] = "Giao dịch bị hủy. Vui lòng thử lại!";

            return RedirectToAction("Index", "Booking");
        }
    }
}
