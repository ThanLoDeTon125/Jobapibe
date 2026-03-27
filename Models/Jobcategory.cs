using System.ComponentModel.DataAnnotations.Schema;

namespace JobHubPro.Api.Models
{
    [Table("job_categories")]
    public class JobCategory
    {
        [Column("job_id")]
        public int JobId { get; set; }

        [Column("category_id")]
        public int CategoryId { get; set; }

        public Job? Job { get; set; }
        public Category? Category { get; set; }
    }
}