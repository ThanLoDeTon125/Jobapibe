using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CandidateProfile? CandidateProfile { get; set; }

    public virtual Company? Company { get; set; }
}
