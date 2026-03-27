using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Companies
                .Include(x => x.User)
                .Include(x => x.Jobs)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Companies
                .Include(x => x.User)
                .Include(x => x.Jobs)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Company model)
        {
            _context.Companies.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Company model)
        {
            var item = await _context.Companies.FindAsync(id);
            if (item == null) return NotFound();

            item.UserId = model.UserId;
            item.CompanyName = model.CompanyName;
            item.TaxCode = model.TaxCode;
            item.Website = model.Website;
            item.CompanySize = model.CompanySize;
            item.Address = model.Address;
            item.Description = model.Description;
            item.LogoUrl = model.LogoUrl;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Companies.FindAsync(id);
            if (item == null) return NotFound();

            _context.Companies.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}