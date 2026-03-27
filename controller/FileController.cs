[HttpGet]
// Bổ sung các tham số tìm kiếm và phân trang
public async Task<IActionResult> GetAll(
    [FromQuery] string? keyword, 
    [FromQuery] string? location, 
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 10)
{
    var query = _context.Jobs
        .Include(x => x.Company)
        .Where(x => x.Status == "ACTIVE") // Chỉ lấy job đang mở
        .AsQueryable();

    // Lọc theo từ khóa (Tìm trong Title)
    if (!string.IsNullOrEmpty(keyword))
        query = query.Where(x => x.Title.Contains(keyword));

    // Lọc theo địa điểm
    if (!string.IsNullOrEmpty(location))
        query = query.Where(x => x.Location.Contains(location));

    // Đếm tổng số lượng để frontend làm phân trang
    var totalItems = await query.CountAsync();

    // Thực hiện phân trang
    var data = await query
        .OrderByDescending(x => x.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Ok(new {
        TotalItems = totalItems,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
        Data = data
    });
}