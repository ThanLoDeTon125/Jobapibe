using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class Bookmark
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public int JobId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;
}
