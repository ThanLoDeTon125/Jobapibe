using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stats")]
    [Authorize(Roles = "ADMIN")] // Chỉ Admin mới xem được số liệu này
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Đếm tổng số lượng trong Database
            var totalUsers = await _context.Users.CountAsync();
            var totalJobs = await _context.Jobs.CountAsync();
            var totalApplications = await _context.Applications.CountAsync();
            var totalCompanies = await _context.Companies.CountAsync();
            var totalReviews = await _context.JobReviews.CountAsync();

            return Ok(new {
                totalUsers,
                totalJobs,
                totalApplications,
                totalCompanies,
                totalReviews
            });
        }
    }
}