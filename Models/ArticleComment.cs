using System;

namespace JobHubPro.Api.Models
{
    public partial class ArticleComment
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Article? Article { get; set; }
        public virtual User? User { get; set; }
    }
}