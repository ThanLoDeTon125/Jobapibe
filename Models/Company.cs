using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class Company
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? TaxCode { get; set; }

    public string? Website { get; set; }

    public string? CompanySize { get; set; }

    public string? Address { get; set; }

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual User User { get; set; } = null!;
}
