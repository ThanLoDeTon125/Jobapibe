using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Applications
                .Include(x => x.Job)
                .Include(x => x.Candidate)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Applications
                .Include(x => x.Job)
                .Include(x => x.Candidate)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

       [HttpPost]
[Authorize(Roles = "CANDIDATE")] // Bắt buộc là ứng viên
public async Task<IActionResult> Create(Application model)
{
    // 1. Lấy UserId từ Token
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                   ?? User.FindFirst("sub");
    if (userIdClaim == null) return Unauthorized();
    int userId = int.Parse(userIdClaim.Value);

    // 2. Lấy Candidate Profile của User này
    var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
    if (candidate == null) return BadRequest("Vui lòng cập nhật hồ sơ ứng viên trước khi ứng tuyển.");

    // 3. Ép cứng CandidateId bằng ID chính chủ, không dùng dữ liệu client gửi lên
    model.CandidateId = candidate.Id;
    model.AppliedAt = DateTime.UtcNow;
    model.UpdatedAt = DateTime.UtcNow;
    model.Status = "PENDING"; // Luôn mặc định là PENDING khi mới nộp

    _context.Applications.Add(model);
    await _context.SaveChangesAsync();
    return Ok(model);
}

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Application model)
        {
            var item = await _context.Applications.FindAsync(id);
            if (item == null) return NotFound();

            // Chỉ cho phép employer/admin cập nhật trạng thái đơn ứng tuyển
            item.Status = model.Status;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Applications.FindAsync(id);
            if (item == null) return NotFound();

            _context.Applications.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}