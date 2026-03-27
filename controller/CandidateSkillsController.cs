using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CandidateSkillsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CandidateSkillsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? candidateId)
        {
            var query = _context.CandidateSkills
                .Include(x => x.Skill)
                .AsQueryable();

            if (candidateId.HasValue)
                query = query.Where(x => x.CandidateId == candidateId.Value);

            var data = await query.ToListAsync();
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CandidateSkill model)
        {
            var exists = await _context.CandidateSkills
                .AnyAsync(cs => cs.CandidateId == model.CandidateId && cs.SkillId == model.SkillId);
            if (exists) return Conflict(new { message = "Kỹ năng này đã được thêm vào hồ sơ." });

            _context.CandidateSkills.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("{candidateId}/{skillId}")]
        public async Task<IActionResult> Update(int candidateId, int skillId, [FromBody] CandidateSkill model)
        {
            var item = await _context.CandidateSkills.FindAsync(candidateId, skillId);
            if (item == null) return NotFound(new { message = "Không tìm thấy kỹ năng này." });

            item.Level = model.Level;
            await _context.SaveChangesAsync();
            
            return Ok(item);
        }

        [HttpDelete("{candidateId}/{skillId}")]
        public async Task<IActionResult> Delete(int candidateId, int skillId)
        {
            var item = await _context.CandidateSkills.FindAsync(candidateId, skillId);
            if (item == null) return NotFound(new { message = "Không tìm thấy kỹ năng này." });

            _context.CandidateSkills.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }
    }
}