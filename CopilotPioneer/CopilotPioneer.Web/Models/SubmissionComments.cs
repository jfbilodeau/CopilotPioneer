namespace CopilotPioneer.Web.Models;

public class SubmissionComments
{
    // Submission ID
    public string Id { get; set; } = string.Empty;
    
    public List<Comment> Comments { get; set; } = [];
}