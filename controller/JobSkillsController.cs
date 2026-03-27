using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSkillsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobSkillsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.JobSkills
                .Include(x => x.Job)
                .Include(x => x.Skill)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobSkill model)
        {
            _context.JobSkills.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpDelete("{jobId}/{skillId}")]
        public async Task<IActionResult> Delete(int jobId, int skillId)
        {
            var item = await _context.JobSkills.FindAsync(jobId, skillId);
            if (item == null) return NotFound();

            _context.JobSkills.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }
    }
}