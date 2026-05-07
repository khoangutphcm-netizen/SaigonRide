using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models.entities
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(100, ErrorMessage = "Tiêu đề không quá 100 ký tự")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; }

        // --- THÊM DÒNG NÀY ĐỂ LƯU ĐƯỜNG DẪN HÌNH ẢNH ---
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}