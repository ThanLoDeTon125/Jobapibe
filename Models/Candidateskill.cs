using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class CandidateSkill
{
    public int CandidateId { get; set; }

    public int SkillId { get; set; }

    public string? Level { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
