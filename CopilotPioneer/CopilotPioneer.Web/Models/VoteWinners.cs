namespace CopilotPioneer.Web.Models;

public class WinnerProfile
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class VoteWinners
{
    public List<WinnerProfile> DailyWinners { get; set; } = [];
    public List<WinnerProfile> WeeklyWinners { get; set; } = [];
}