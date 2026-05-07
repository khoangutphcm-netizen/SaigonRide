using Microsoft.EntityFrameworkCore;
using SaigonRide.Models.entities;
using SaigonRide.Models.Enums;
using System; // Nhớ có dòng này để dùng DateTime

namespace SaigonRide.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Station> Stations { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<RentalTransaction> RentalTransactions { get; set; }
        public DbSet<User> Users { get; set; }

        // Đã xóa 1 dòng Announcements bị trùng
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RentalTransaction>()
                .HasOne(rt => rt.Vehicle).WithMany().HasForeignKey(rt => rt.VehicleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentalTransaction>()
                .HasOne(rt => rt.ReturnStation).WithMany().HasForeignKey(rt => rt.ReturnStationId).OnDelete(DeleteBehavior.Restrict);
            // SỬA CASCADE THÀNH RESTRICT ĐỂ CHỐNG LỖI VÒNG LẶP (CYCLES)
            modelBuilder.Entity<Report>()
                .HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.RentalTransaction).WithMany().HasForeignKey(r => r.RentalTransactionId).OnDelete(DeleteBehavior.Restrict);

            // Đã xóa thuộc tính 'Type' không tồn tại để tránh lỗi
            modelBuilder.Entity<Announcement>().HasData(
                new Announcement { Id = 1, Title = "Thả ga vi vu cuối tuần!", Content = "Giảm ngay 15% khi trả xe tại trạm Bình Thạnh.", IsActive = true, CreatedAt = new DateTime(2026, 4, 30) },
                new Announcement { Id = 2, Title = "Bảo trì trạm Quận 1", Content = "Cuối tuần này trạm Quận 1 sẽ tạm ngưng nhận xe trả để nâng cấp.", IsActive = true, CreatedAt = new DateTime(2026, 4, 30) }
            );

            // Seed Data cho Station
            modelBuilder.Entity<Station>().HasData(
                new Station { Id = 1, LocationName = "District 1, HCM", MaxCapacity = 50, CurrentInventory = 42 },
                new Station { Id = 3, LocationName = "Binh Thanh, HCM", MaxCapacity = 20, CurrentInventory = 3 }
            );

            // Seed Data cho Vehicle
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle { Id = 1, Category = "Standard Bike", PricePerMinute = 500, StationId = 1, Status = VehicleStatus.Available },
                new Vehicle { Id = 2, Category = "E-Scooter", PricePerMinute = 1500, StationId = 1, Status = VehicleStatus.Available },
                new Vehicle { Id = 3, Category = "Premium E-Bike", PricePerMinute = 2000, StationId = 3, Status = VehicleStatus.UnderMaintenance }
            );

            // Seed 1 tài khoản Admin
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "System Admin", Email = "admin@saigonride.com", Password = "123", UserType = "Admin", DocumentId = "000000000" }
            );
        }
    }
}