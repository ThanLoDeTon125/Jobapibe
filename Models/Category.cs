using System;
using System.Collections.Generic;

namespace JobHubPro.Api.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<JobCategory> JobCategories { get; set; } = new List<JobCategory>();
}
