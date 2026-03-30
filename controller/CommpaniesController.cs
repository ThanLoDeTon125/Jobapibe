using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        // Ai cũng có thể xem danh sách các công ty
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Companies.ToListAsync();
            return Ok(data);
        }

        // Ai cũng có thể xem chi tiết một công ty
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Companies
                .Include(c => c.Jobs) // Lấy luôn danh sách việc làm của công ty này
                .FirstOrDefaultAsync(c => c.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

        // CHỈ EMPLOYER mới được tạo hồ sơ công ty
        [HttpPost]
        [Authorize(Roles = "EMPLOYER")]
        public async Task<IActionResult> Create(Company model)
        {
            // 1. Lấy UserId từ Token đang đăng nhập
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");

            // 2. Kiểm tra xem User này đã tạo hồ sơ công ty chưa (Quan hệ 1-1)
            var exists = await _context.Companies.AnyAsync(c => c.UserId == userId);
            if (exists) 
                return BadRequest(new { message = "Bạn đã tạo hồ sơ công ty rồi. Hãy sử dụng chức năng Cập nhật." });

            // 3. Gán UserId bằng ID của người đang đăng nhập, bỏ qua ID client gửi lên
            model.UserId = userId;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Companies.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        // EMPLOYER tự sửa công ty của mình, hoặc ADMIN có quyền sửa đổi
        [HttpPut("{id}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Update(int id, Company model)
        {
            var item = await _context.Companies.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Kiểm tra quyền sở hữu: Nếu không phải Admin và cũng không phải chủ công ty thì cấm
            if (userRole != "ADMIN" && item.UserId != userId)
            {
                return Forbid("Bạn không có quyền chỉnh sửa hồ sơ của công ty này.");
            }

            // Cập nhật các trường thông tin (Tuyệt đối KHÔNG cập nhật item.UserId)
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

        // Xóa công ty (Kèm theo điều kiện bảo mật tương tự Update)
        [HttpDelete("{id}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Companies.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.UserId != userId)
            {
                return Forbid("Bạn không có quyền xóa hồ sơ của công ty này.");
            }

            _context.Companies.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa hồ sơ công ty thành công." });
        }
    }
}