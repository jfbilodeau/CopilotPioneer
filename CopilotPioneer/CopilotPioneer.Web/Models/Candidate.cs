namespace CopilotPioneer.Web.Models;

public class Candidate
{
    public bool DailyVoteCast { get; set; }
    public bool WeeklyVoteCast { get; set; }
    
    public List<Submission> DailySubmissions { get; set; } = [];
    public List<Submission> WeeklySubmissions { get; set; } = [];
}