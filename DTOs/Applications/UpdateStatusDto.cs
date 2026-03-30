using System.ComponentModel.DataAnnotations;

namespace JobHubPro.Api.DTOs.Applications
{
    public class UpdateStatusDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }
}