using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class CandidateProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public int ExperienceYears { get; set; }

    public string? Education { get; set; }

    public string? CurrentPosition { get; set; }

    public string? DesiredPosition { get; set; }

    public decimal? DesiredSalary { get; set; }

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public string? CvUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();

    public virtual User User { get; set; } = null!;
}
