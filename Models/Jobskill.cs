using System.ComponentModel.DataAnnotations.Schema;

namespace JobHubPro.Api.Models
{
    [Table("job_skills")]
    public class JobSkill
    {
        [Column("job_id")]
        public int JobId { get; set; }

        [Column("skill_id")]
        public int SkillId { get; set; }

        public Job? Job { get; set; }
        public Skill? Skill { get; set; }
    }
}