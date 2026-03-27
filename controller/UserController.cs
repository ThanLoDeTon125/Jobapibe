using JobHubPro.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "ADMIN")] // Toàn bộ Controller này chỉ dành cho Admin
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Nên ẩn PasswordHash trước khi trả về
            var data = await _context.Users
                .Select(u => new { u.Id, u.Email, u.Role, u.Status, u.CreatedAt })
                .ToListAsync();
            return Ok(data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, User model)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return NotFound();

            item.Email = model.Email;
            item.Role = model.Role;
            item.Status = model.Status;
            item.UpdatedAt = DateTime.UtcNow;

            // Xử lý mã hóa nếu Admin có truyền lên mật khẩu mới
            if (!string.IsNullOrEmpty(model.PasswordHash))
            {
                item.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!" });
        }
    }
}