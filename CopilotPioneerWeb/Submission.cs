using System.ComponentModel.DataAnnotations;

namespace CopilotPioneerWeb;

public sealed class Submission
{
    public string Id { get; set; } = "";
    
    public string Author { get; set; } = "";
    public string Product { get; set; } = "";
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    
    [Required]
    public string Title { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Notes { get; set; } = "";
    public byte[][] Images { get; set; } = new byte[3][];
}