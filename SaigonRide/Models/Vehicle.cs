using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; }
        public VehicleStatus Status { get; set; }
    }
}