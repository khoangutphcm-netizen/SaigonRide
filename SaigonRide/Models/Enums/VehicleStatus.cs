// Models/Enums/VehicleStatus.cs
namespace SaigonRide.Models.Enums
{
    public enum VehicleStatus
    {
        Available = 0,         // Sẵn sàng
        InUse = 1,             // Đang được thuê
        UnderMaintenance = 2,  // Đang bảo trì
        Unavailable = 3        // Không khả dụng (Mất cắp, hỏng nặng...)
    }
}