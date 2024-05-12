namespace CopilotPioneer.Web.Models;

public class Profile
{
    public string Id { get; set; } = string.Empty;
    public string? DisplayName { get; set; } = string.Empty;
    public string Name => DisplayName ?? Id;
    
    public int Points { get; set; } = 0;
}