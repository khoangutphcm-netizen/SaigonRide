using System;

namespace SaigonRide.Models.entities
{
    public class PendingRentalOrder
    {
        public int VehicleId { get; set; }
        public int ReturnStationId { get; set; }
        public int Duration { get; set; }
        public decimal FinalPrice { get; set; }
        public int CurrentUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Hiển thị
        public string VehicleName { get; set; }
        public string ReturnStationName { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsDiscountApplied { get; set; }

        // MỚI THÊM: Lưu phương thức khách vừa chọn ở Receipt
        public string PaymentMethod { get; set; }
    }
}