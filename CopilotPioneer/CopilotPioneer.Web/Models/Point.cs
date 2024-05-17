using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CopilotPioneer.Web.Models;

public enum PointType
{
    Submission = 1,
    Vote,
}

public class Point
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    [JsonConverter(typeof(StringEnumConverter))]
    public PointType Type { get; set; } = PointType.Submission;
    public int Amount { get; set; } = 0;
}
