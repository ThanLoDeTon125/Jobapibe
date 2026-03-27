using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

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

        // Ai cũng có thể xem danh sách việc làm
        [HttpGet]
        [AllowAnonymous] 
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Jobs.Include(x => x.Company).ToListAsync();
            return Ok(data);
        }

        // Chỉ Employer hoặc Admin mới được tạo việc làm
        [HttpPost]
        [Authorize(Roles = "EMPLOYER, ADMIN")] 
        public async Task<IActionResult> Create(Job model)
        {
            // 1. Lấy UserId từ JWT Token của người đang gọi API
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");

            // 2. Tìm Công ty (Company) thuộc về UserId này
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (company == null) 
                return BadRequest(new { message = "Bạn cần cập nhật hồ sơ công ty trước khi đăng tin tuyển dụng." });

            // 3. Ghi đè CompanyId bằng ID chính chủ, không dùng dữ liệu client gửi
            model.CompanyId = company.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Jobs.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        // Tương tự, chỉ cho phép xóa Job nếu Job đó thuộc về công ty của Employer đang đăng nhập
        [HttpDelete("{id}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            // Kiểm tra quyền sở hữu: Trừ phi là Admin, Employer chỉ được xóa job của mình
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ADMIN" && item.CompanyId != company?.Id)
            {
                return Forbid("Bạn không có quyền xóa tin tuyển dụng của công ty khác.");
            }

            _context.Jobs.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa việc làm thành công" });
        }
    }
}