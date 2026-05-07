using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using SaigonRide.Models.entities;
using System;
using System.IO;
using System.Linq;
using System.Text.Json; // Bắt buộc để gói dữ liệu Chart
using System.Threading.Tasks;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminDashboardController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
   
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            ViewBag.TotalUsers = _context.Users.Where(u => u.UserType != "Admin").Count();
            ViewBag.TotalStations = _context.Stations.Count();
            ViewBag.TotalVehicles = _context.Vehicles.Count();
            ViewBag.TotalTransactions = _context.RentalTransactions.Count();
             
            var stations = _context.Stations.ToList();
            var stationLabels = stations.Select(s => s.LocationName).ToArray();
            var inventoryData = stations.Select(s => s.CurrentInventory).ToArray();

            var inventoryColors = stations.Select(s => {
                if (s.MaxCapacity == 0) return "#4e73df";
                var pct = (double)s.CurrentInventory / s.MaxCapacity;
                if (pct <= 0.2) return "#e74a3b";
                if (pct >= 0.6) return "#1cc88a";
                return "#4e73df";
            }).ToArray();

            ViewBag.StationLabels = JsonSerializer.Serialize(stationLabels);
            ViewBag.InventoryData = JsonSerializer.Serialize(inventoryData);
            ViewBag.InventoryColors = JsonSerializer.Serialize(inventoryColors);
             
            var transactions = _context.RentalTransactions.Include(rt => rt.Vehicle).ToList();

            decimal bikeRevenue = transactions.Where(rt => !rt.Vehicle.Category.Contains("Scooter")).Sum(rt => rt.TotalDue);
            decimal scooterRevenue = transactions.Where(rt => rt.Vehicle.Category.Contains("Scooter")).Sum(rt => rt.TotalDue);

            ViewBag.BikeRevenue = bikeRevenue;
            ViewBag.ScooterRevenue = scooterRevenue;
            ViewBag.TotalRevenue = bikeRevenue + scooterRevenue;

            ViewBag.RevenueLabels = JsonSerializer.Serialize(new[] { "Xe đạp", "Xe điện" });
            ViewBag.RevenueData = JsonSerializer.Serialize(new[] { bikeRevenue, scooterRevenue });
 
            return View();
        }

        [HttpGet]
        public IActionResult Announcements(string searchString)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            var announcements = _context.Announcements.AsQueryable();

            // LOGIC TÌM KIẾM
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                announcements = announcements.Where(a =>
                    a.Title.ToLower().Contains(searchString) ||
                    a.Content.ToLower().Contains(searchString));
            }

            ViewBag.CurrentSearch = searchString;
            return View(announcements.OrderByDescending(a => a.CreatedAt).ToList());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(Announcement model, IFormFile? imageFile)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");

            ModelState.Remove("ImageUrl");
            ModelState.Remove("Id");

            try
            {
                // Validate dữ liệu input
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    TempData["Error"] = "Lỗi: Tiêu đề không được để trống!";
                    return RedirectToAction("Announcements");
                }

                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    TempData["Error"] = "Lỗi: Nội dung không được để trống!";
                    return RedirectToAction("Announcements");
                }

                // Xử lý upload ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        model.ImageUrl = await UploadImage(imageFile);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Lỗi upload ảnh: {ex.Message}";
                        return RedirectToAction("Announcements");
                    }
                }

                // Đặt thời gian tạo và trạng thái
                model.CreatedAt = DateTime.Now;

                // Nếu IsActive không được set, mặc định là true
                if (model.IsActive == false) // Nếu unchecked
                {
                    model.IsActive = false;
                }
                else
                {
                    model.IsActive = true;
                }

                // Lưu vào database
                _context.Announcements.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Đã đăng thông báo mới thành công!";
            }
            catch (Exception ex)
            { 
                TempData["Error"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToAction("Announcements");
        }

        [HttpGet]
        public async Task<IActionResult> EditAnnouncement(int? id)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();
            var ann = await _context.Announcements.FindAsync(id);
            if (ann == null) return NotFound();
            return View(ann);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAnnouncement(Announcement model, IFormFile? imageFile)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");
            if (ModelState.IsValid)
            {
                var annInDb = await _context.Announcements.FindAsync(model.Id);
                if (annInDb != null)
                {
                    if (imageFile != null)
                    {
                        if (!string.IsNullOrEmpty(annInDb.ImageUrl)) DeleteImage(annInDb.ImageUrl);
                        annInDb.ImageUrl = await UploadImage(imageFile);
                    }
                    annInDb.Title = model.Title;
                    annInDb.Content = model.Content;
                    annInDb.IsActive = model.IsActive;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã cập nhật thông báo!";
                }
            }
            return RedirectToAction("Announcements");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            if (User.FindFirst("UserType")?.Value != "Admin") return RedirectToAction("Index", "Home");
            var ann = await _context.Announcements.FindAsync(id);
            if (ann != null)
            {
                if (!string.IsNullOrEmpty(ann.ImageUrl)) DeleteImage(ann.ImageUrl);
                _context.Announcements.Remove(ann);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thông báo thành công!";
            }
            return RedirectToAction("Announcements");
        }

        private async Task<string> UploadImage(IFormFile file)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string path = Path.Combine(wwwRootPath, @"uploads\announcements");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return @"\uploads\announcements\" + fileName;
        }

        private void DeleteImage(string imageUrl)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string imagePath = Path.Combine(wwwRootPath, imageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
        }
    }
}