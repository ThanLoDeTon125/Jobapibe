using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Jobs
                .Include(x => x.Company)
                .Include(x => x.JobSkills)
                    .ThenInclude(x => x.Skill)
                .Include(x => x.JobCategories)
                    .ThenInclude(x => x.Category)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Jobs
                .Include(x => x.Company)
                .Include(x => x.JobSkills)
                    .ThenInclude(x => x.Skill)
                .Include(x => x.JobCategories)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Applications)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Job model)
        {
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            _context.Jobs.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Job model)
        {
            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            item.CompanyId = model.CompanyId;
            item.Title = model.Title;
            item.Slug = model.Slug;
            item.Description = model.Description;
            item.Requirements = model.Requirements;
            item.Benefits = model.Benefits;
            item.Location = model.Location;
            item.EmploymentType = model.EmploymentType;
            item.ExperienceLevel = model.ExperienceLevel;
            item.SalaryMin = model.SalaryMin;
            item.SalaryMax = model.SalaryMax;
            item.Quantity = model.Quantity;
            item.Deadline = model.Deadline;
            item.Status = model.Status;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            _context.Jobs.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}