using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobCategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.JobCategories
                .Include(x => x.Job)
                .Include(x => x.Category)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobCategory model)
        {
            _context.JobCategories.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpDelete("{jobId}/{categoryId}")]
        public async Task<IActionResult> Delete(int jobId, int categoryId)
        {
            var item = await _context.JobCategories.FindAsync(jobId, categoryId);
            if (item == null) return NotFound();

            _context.JobCategories.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }
    }
}