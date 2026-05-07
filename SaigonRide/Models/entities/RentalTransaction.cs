using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonRide.Models.entities
{
    public class RentalTransaction
    {
        public int Id { get; set; }

        // THE NEW FOREIGN KEY
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalDue { get; set; }

        public bool IsDiscountApplied { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }
        public int ReturnStationId { get; set; }
        public Station ReturnStation { get; set; }
    }
}