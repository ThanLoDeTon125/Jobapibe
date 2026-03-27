using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class Job
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Requirements { get; set; }

    public string? Benefits { get; set; }

    public string? Location { get; set; }

    public string EmploymentType { get; set; } = null!;

    public string ExperienceLevel { get; set; } = null!;

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public int Quantity { get; set; }

    public DateTime? Deadline { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<JobCategory> JobCategories { get; set; } = new List<JobCategory>();

    public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
