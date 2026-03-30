using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/files")]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FileController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        [Authorize] // Bắt buộc đăng nhập mới được upload
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) 
                return BadRequest("Không tìm thấy file.");
            
            // Lấy đường dẫn thư mục wwwroot/uploads
            var uploadFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder)) 
                Directory.CreateDirectory(uploadFolder);

            // Tạo tên file ngẫu nhiên để tránh trùng lặp
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về URL để frontend lưu vào database (AvatarUrl, CvUrl...)
            var url = $"/uploads/{fileName}";
            return Ok(new { Url = url });
        }
    }
}