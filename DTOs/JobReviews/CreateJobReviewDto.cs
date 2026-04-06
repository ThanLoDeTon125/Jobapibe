namespace JobHubPro.Api.DTOs.JobReviews
{
    public class CreateJobReviewDto
    {
        public int JobId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }
}