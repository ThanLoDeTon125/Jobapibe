using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;
using JobHubPro.Api.DTOs.Jobs;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobsController(AppDbContext context)
        {
            _context = context;
        }

[HttpGet]
[AllowAnonymous] 
public async Task<IActionResult> GetAll(
    [FromQuery] string? keyword, 
    [FromQuery] string? location, 
    [FromQuery] decimal? minSalary,
    [FromQuery] int? categoryId,
    [FromQuery] string? employmentType)
{
    var query = _context.Jobs.Include(x => x.Company).AsQueryable();

    if (!string.IsNullOrEmpty(keyword))
        query = query.Where(x => x.Title != null && x.Title.Contains(keyword));
    
    if (!string.IsNullOrEmpty(location))
        query = query.Where(x => x.Location != null && x.Location.Contains(location));
    
    if (minSalary.HasValue)
        query = query.Where(x => x.SalaryMin != null && x.SalaryMin >= minSalary.Value);

    if (categoryId.HasValue)
        query = query.Where(x => x.JobCategories.Any(jc => jc.CategoryId == categoryId.Value));

    if (!string.IsNullOrEmpty(employmentType))
        query = query.Where(x => x.EmploymentType == employmentType);

    var data = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    return Ok(data);
}

       [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Jobs.Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "EMPLOYER, ADMIN")] 
        public async Task<IActionResult> Create([FromBody] createJobDto model)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (company == null) 
                return BadRequest(new { message = "Bạn cần cập nhật hồ sơ công ty trước khi đăng tin tuyển dụng." });

            model.CompanyId = company.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Jobs.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        [HttpPut("{id}")]
[Authorize(Roles = "EMPLOYER, ADMIN")]
public async Task<IActionResult> Update(int id, [FromBody] updateJobDto model)
        {
            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.CompanyId != company?.Id)
                return Forbid("Bạn không có quyền sửa tin này.");

            item.Title = model.Title;
            item.Description = model.Description;
            item.Requirements = model.Requirements;
            item.Location = model.Location;
            item.SalaryMin = model.SalaryMin;
            item.SalaryMax = model.SalaryMax;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ADMIN" && item.CompanyId != company?.Id)
                return Forbid("Bạn không có quyền xóa tin tuyển dụng của công ty khác.");

            _context.Jobs.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa việc làm thành công" });
        }
    }
}