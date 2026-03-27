namespace RecruitmentApi.Models
{
    public class CreateJobRequest
    {
        public int CompanyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Requirements { get; set; }
        public string? Benefits { get; set; }
        public string? Location { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public string ExperienceLevel { get; set; } = string.Empty;
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public int Quantity { get; set; }
        public DateTime? Deadline { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}