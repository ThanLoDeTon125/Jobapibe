using System;

namespace JobHubPro.Api.Models
{
    public partial class JobReview
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int CandidateId { get; set; }
        
        // Số sao đánh giá từ 1 đến 5
        public int Rating { get; set; } 
        public string? Comment { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Job? Job { get; set; }
        public virtual CandidateProfile? Candidate { get; set; }
    }
}