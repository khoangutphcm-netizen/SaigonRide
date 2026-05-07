using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaigonRide.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetNotificationCount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            int userId = int.Parse(userIdString);

            var count = await _context.Reports
                .Where(r => r.UserId == userId && !string.IsNullOrEmpty(r.AdminResponse) && r.Status != SaigonRide.Models.entities.ReportStatus.Closed)
                .CountAsync();

            return Ok(new { count });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            int userId = int.Parse(userIdString);

            var notifications = await _context.Reports
                .Where(r => r.UserId == userId && !string.IsNullOrEmpty(r.AdminResponse))
                .OrderByDescending(r => r.RespondedAt)
                .Take(5)
                .Select(r => new
                {
                    reportId = r.Id,
                    title = r.Title,
                    status = r.Status.ToString(),
                    respondedAt = r.RespondedAt.Value.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(notifications);
        }
    }
}
