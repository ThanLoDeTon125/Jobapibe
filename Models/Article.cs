using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models
{
    public partial class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        
        // Khóa ngoại trỏ đến người đăng (Thường là Admin hoặc Employer)
        public int AuthorId { get; set; } 
        
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User? Author { get; set; }
        public virtual ICollection<ArticleComment> ArticleComments { get; set; } = new List<ArticleComment>();
    }
}