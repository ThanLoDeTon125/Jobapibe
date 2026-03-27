using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class Application
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int CandidateId { get; set; }

    public string? CoverLetter { get; set; }

    public string? CvUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime AppliedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;
}
