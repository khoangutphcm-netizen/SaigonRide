using System.ComponentModel.DataAnnotations.Schema;
using SaigonRide.Models.Enums;

namespace SaigonRide.Models.entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Category { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerMinute { get; set; }

        // Biến mới: Trạng thái của xe (Mặc định khi tạo ra là Available)
        public VehicleStatus Status { get; set; } = VehicleStatus.Available;

        public int StationId { get; set; }
        public Station Station { get; set; }
    }
}