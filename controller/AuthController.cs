using JobHubPro.Api.DTOs.Auth;
using JobHubPro.Api.Models;
using JobHubPro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")] // Chuẩn hóa route
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

            if (user.Status == "INACTIVE")
                return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa." });

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Role = user.Role,
                UserId = user.Id,
                Email = user.Email
            });
        }

        // Endpoint Logout (Chủ yếu để frontend gọi và tự xóa token dưới LocalStorage/Cookies)
        [HttpPost("logout")]
        [Authorize] // Phải có token mới cho logout
        public IActionResult Logout()
        {
            // Do JWT là stateless, backend không cần làm gì ngoài việc báo thành công.
            // Client sẽ tự hủy token.
            return Ok(new { message = "Đăng xuất thành công. Vui lòng xóa token ở client." });
        }

        // Lấy thông tin người dùng đang đăng nhập
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            // Lấy UserId từ Token (claim "sub" đã được setup trong JwtService)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users
                .Include(u => u.CandidateProfile)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Ẩn PasswordHash trước khi trả về
            user.PasswordHash = string.Empty; 

            return Ok(user);
        }

        /// <summary>Đăng ký tài khoản mới</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return Conflict(new { message = "Email đã được sử dụng." });

            var allowedRoles = new[] { "CANDIDATE", "EMPLOYER" };
            if (!allowedRoles.Contains(dto.Role.ToUpper()))
                return BadRequest(new { message = "Role không hợp lệ. Chỉ chấp nhận CANDIDATE hoặc EMPLOYER." });

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role.ToUpper(),
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Role = user.Role,
                UserId = user.Id,
                Email = user.Email
            });
        }
    }
}