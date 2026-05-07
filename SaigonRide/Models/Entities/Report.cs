using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models.entities
{
    public class Report
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int RentalTransactionId { get; set; }
        public RentalTransaction RentalTransaction { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [StringLength(500)]
        public string AdminResponse { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? RespondedAt { get; set; }
    }

    public enum ReportStatus
    {
        Open = 0,
        InProgress = 1,
        Resolved = 2,
        Closed = 3
    }
}
