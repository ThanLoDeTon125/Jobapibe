using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookmarksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookmarksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? candidateId)
        {
            var query = _context.Bookmarks
                .Include(x => x.Candidate)
                .Include(x => x.Job)
                .AsQueryable();

            if (candidateId.HasValue)
                query = query.Where(x => x.CandidateId == candidateId.Value);

            return Ok(await query.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Bookmarks
                .Include(x => x.Candidate)
                .Include(x => x.Job)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Bookmark model)
        {
            // Kiểm tra trùng bookmark
            var exists = await _context.Bookmarks
                .AnyAsync(b => b.CandidateId == model.CandidateId && b.JobId == model.JobId);
            if (exists)
                return Conflict(new { message = "Việc làm đã được lưu trước đó." });

            model.CreatedAt = DateTime.UtcNow;
            _context.Bookmarks.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Bookmarks.FindAsync(id);
            if (item == null) return NotFound();

            _context.Bookmarks.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}