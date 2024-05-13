using System.ComponentModel.DataAnnotations;

namespace CopilotPioneer.Web.Models;

public class Profile
{
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(40)]
    public string? Name { get; set; } = string.Empty;
    
    public int Points { get; set; } = 0;
    
    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(Name) ? Id : Name;
    }
}