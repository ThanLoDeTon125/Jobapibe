using JobHubPro.Api.DTOs.Auth;
using JobHubPro.Api.Models;
using JobHubPro.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;

namespace JobHubPro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    /// <summary>Đăng nhập — trả về JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        // Kiểm tra password hash (BCrypt)
        var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isValid)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        if (user.Status == "INACTIVE")
            return Unauthorized(new { message = "Tài khoản đã bị khóa." });

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Role = user.Role,
            UserId = user.Id,
            Email = user.Email
        });
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

        return CreatedAtAction(nameof(Login), new AuthResponse
        {
            Token = token,
            Role = user.Role,
            UserId = user.Id,
            Email = user.Email
        });
    }
}
