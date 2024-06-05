using Newtonsoft.Json;

namespace CopilotPioneer.Web.Models;

public class Screenshot
{
    public string Id { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string SubmissionId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string HeroName { get; set; } = string.Empty;

    public string ThumbnailName { get; set; } = string.Empty;

    // True if a document (Word, PDF, etc.), false if an image
    public bool IsDocument { get; set; } = false;
}