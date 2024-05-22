using System.ComponentModel.DataAnnotations;

namespace CopilotPioneer.Web.Models;

public class Comment
{
    public string Id { get; set; } = string.Empty;
    
    public string Author { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    [Required]
    public string Content { get; set; } = string.Empty;
}