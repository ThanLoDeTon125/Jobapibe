using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    // 1. TẠO CÁI PHỄU (DTO) CHỈ NHẬN NHỮNG DỮ LIỆU FRONTEND GỬI LÊN
    // Giúp loại bỏ hoàn toàn lỗi 400 Bad Request
    public class CompanyInputDto
    {
        public string CompanyName { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? CompanySize { get; set; }
        public string? Description { get; set; }
    }

    [ApiController]
    [Route("api/v1/companies")]
    public class CommpaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommpaniesController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả công ty
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Companies
                .Select(c => new {
                    c.Id, c.CompanyName, c.LogoUrl, c.Website, c.Address, c.CompanySize, c.Description
                })
                .ToListAsync();
            return Ok(data);
        }

        // Lấy thông tin công ty của User (HR) đang đăng nhập
        [HttpGet("my-company")]
        [Authorize]
        public async Task<IActionResult> GetMyCompany()
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(claimId) || !int.TryParse(claimId, out int userId))
                return Unauthorized();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            
            // Nếu chưa có công ty thì trả về 200 kèm data rỗng (để Frontend hiển thị Form thêm mới)
            if (company == null) return Ok(null);

            return Ok(new {
                company.Id, company.CompanyName, company.LogoUrl, company.Website, 
                company.Address, company.CompanySize, company.Description
            });
        }

        // Xem chi tiết công ty theo ID
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            
            return Ok(new {
                company.Id, company.CompanyName, company.LogoUrl, company.Website, 
                company.Address, company.CompanySize, company.Description
            });
        }

        // Thêm mới hồ sơ công ty
        [HttpPost]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Create([FromBody] CompanyInputDto model)
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(claimId) || !int.TryParse(claimId, out int userId))
                return Unauthorized(new { message = "Không xác định được danh tính." });

            var existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (existingCompany != null)
                return BadRequest(new { message = "Tài khoản này đã tạo hồ sơ công ty rồi." });

            var company = new Company
            {
                UserId = userId,
                CompanyName = model.CompanyName,
                LogoUrl = model.LogoUrl,
                Website = model.Website,
                Address = model.Address,
                CompanySize = model.CompanySize,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo hồ sơ công ty thành công!", id = company.Id });
        }

        // Cập nhật hồ sơ công ty
        [HttpPut("{id}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] CompanyInputDto model)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && company.UserId != userId)
                return Forbid();

            company.CompanyName = model.CompanyName;
            company.LogoUrl = model.LogoUrl;
            company.Website = model.Website;
            company.Address = model.Address;
            company.CompanySize = model.CompanySize;
            company.Description = model.Description;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật hồ sơ thành công!" });
        }
    }
}