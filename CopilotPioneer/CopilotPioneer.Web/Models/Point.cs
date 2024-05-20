using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CopilotPioneer.Web.Models;

public enum PointType
{
    Submission = 1,
    DailyVote,
    WeeklyVote
}

public class Point
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    [JsonConverter(typeof(StringEnumConverter))]
    public PointType Type { get; set; } = PointType.Submission;
    
    // The frame in which the point was awarded.
    // For example, this would be the date of the daily vote or the week of the weekly vote.
    // (Not the datetime the point was awarded.)
    public string Frame { get; set; } = string.Empty;
    public int Amount { get; set; } = 0;
}
