namespace CopilotPioneer.Web.Models;

public class WinnerProfile
{
    public DateTime Date { get; init; }

    public Profile Profile { get; init; } = new();

    public Submission? Submission { get; init; } = new();
}

public class VoteWinners
{
    public List<WinnerProfile> DailyWinners { get; init; } = [];
    public List<WinnerProfile> WeeklyWinners { get; init; } = [];
}