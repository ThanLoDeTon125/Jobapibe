using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JobHubPro.Api.DTOs.CandidateProfiles; // <-- Gọi file DTO riêng biệt vào đây

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/candidateprofiles")]
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

        [HttpGet("{id:int}")]
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
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> Create([FromBody] CandidateProfileDto model)
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            
            var exists = await _context.CandidateProfiles.AnyAsync(c => c.UserId == userId);
            if (exists) return BadRequest(new { message = "Bạn đã tạo hồ sơ rồi, hãy sử dụng chức năng Cập nhật." });

            var profile = new CandidateProfile
            {
                UserId = userId,
                FullName = model.FullName ?? "Chưa cập nhật tên",
                Phone = model.Phone,
                Address = model.Address,
                ExperienceYears = model.ExperienceYears ?? 0, 
                CvUrl = model.CvUrl,
                Bio = model.Bio,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CandidateProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tạo hồ sơ thành công!", id = profile.Id });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "CANDIDATE, ADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] CandidateProfileDto model)
        {
            var item = await _context.CandidateProfiles.FindAsync(id);
            if (item == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.UserId != userId)
                return Forbid();

            item.FullName = model.FullName ?? item.FullName;
            item.Phone = model.Phone;
            item.Address = model.Address;
            item.ExperienceYears = model.ExperienceYears ?? item.ExperienceYears;
            item.CvUrl = model.CvUrl;
            item.Bio = model.Bio;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật hồ sơ thành công!" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.CandidateProfiles.FindAsync(id);
            if (item == null) return NotFound();

            _context.CandidateProfiles.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa hồ sơ thành công" });
        }
    }
}