using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

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
        public async Task<IActionResult> Create(CandidateProfile model)
        {
            _context.CandidateProfiles.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CandidateProfile model)
        {
            var item = await _context.CandidateProfiles.FindAsync(id);
            if (item == null) return NotFound();

            item.UserId = model.UserId;
            item.FullName = model.FullName;
            item.Phone = model.Phone;
            item.Address = model.Address;
            item.DateOfBirth = model.DateOfBirth;
            item.Gender = model.Gender;
            item.ExperienceYears = model.ExperienceYears;
            item.Education = model.Education;
            item.CurrentPosition = model.CurrentPosition;
            item.DesiredPosition = model.DesiredPosition;
            item.DesiredSalary = model.DesiredSalary;
            item.Bio = model.Bio;
            item.AvatarUrl = model.AvatarUrl;
            item.CvUrl = model.CvUrl;

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