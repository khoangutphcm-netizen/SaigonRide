using Microsoft.AspNetCore.Mvc;
using SaigonRide.Data;
using System.Linq;

namespace SaigonRide.Controllers
{
    public class IntroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IntroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang chủ Intro (Mặc định)
        public IActionResult Index()
        {
            // Kéo 3 thông báo mới nhất đang Active để đưa ra trang Intro
            var recentAnnouncements = _context.Announcements
                                              .Where(a => a.IsActive)
                                              .OrderByDescending(a => a.CreatedAt)
                                              .Take(3)
                                              .ToList();
            return View(recentAnnouncements);
        }

        // Trang Xem tất cả thông báo (Cậu đã làm ở bước trước)
        public IActionResult Announcements()
        {
            var list = _context.Announcements
                               .Where(a => a.IsActive)
                               .OrderByDescending(a => a.CreatedAt)
                               .ToList();
            return View(list);
        }
    }
}