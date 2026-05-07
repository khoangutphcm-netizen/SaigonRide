namespace SaigonRide.Models.entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Ghi chú: Thực tế phải mã hóa Hash, nhưng đồ án ta để dạng chuỗi cho dễ chấm

        public string UserType { get; set; } // "Local", "Tourist" hoặc "Admin"

        public string DocumentId { get; set; } // Chứa số CCCD (nếu là Local) hoặc Passport (nếu là Tourist)
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsBanned { get; set; } = false; // Mặc định tạo tài khoản là không bị khóa
    }
}