using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CandidateProfilesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CandidateProfilesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.CandidateProfiles
                .Include(x => x.User)
                .Include(x => x.CandidateSkills)
                    .ThenInclude(x => x.Skill)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.CandidateProfiles
                .Include(x => x.User)
                .Include(x => x.CandidateSkills)
                    .ThenInclude(x => x.Skill)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

      [HttpPost]
[Authorize(Roles = "CANDIDATE")] // Phải là ứng viên
public async Task<IActionResult> Create(CandidateProfile model)
{
    // Lấy UserId từ Token đang đăng nhập
    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
    
    // Kiểm tra xem User này đã có Profile chưa, nếu có thì không cho tạo thêm
    var exists = await _context.CandidateProfiles.AnyAsync(c => c.UserId == userId);
    if (exists) return BadRequest("Bạn đã tạo hồ sơ rồi, hãy sử dụng chức năng Cập nhật.");

    // Gán UserId bằng người dùng hiện tại
    model.UserId = userId;
    model.CreatedAt = DateTime.UtcNow;
    model.UpdatedAt = DateTime.UtcNow;

    _context.CandidateProfiles.Add(model);
    await _context.SaveChangesAsync();
    return Ok(model);
}

[HttpPut("{id}")]
[Authorize(Roles = "CANDIDATE, ADMIN")]
public async Task<IActionResult> Update(int id, CandidateProfile model)
{
    var item = await _context.CandidateProfiles.FindAsync(id);
    if (item == null) return NotFound();

    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

    // Chặn nếu cố tình sửa hồ sơ của người khác (trừ phi là Admin)
    if (userRole != "ADMIN" && item.UserId != userId)
        return Forbid();

    // KHÔNG cho phép cập nhật item.UserId
    item.FullName = model.FullName;
    item.Phone = model.Phone;
    item.AvatarUrl = model.AvatarUrl;
    item.CvUrl = model.CvUrl;
    // ... cập nhật các trường khác ...
    item.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    return Ok(item);
}

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.CandidateProfiles.FindAsync(id);
            if (item == null) return NotFound();

            _context.CandidateProfiles.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}