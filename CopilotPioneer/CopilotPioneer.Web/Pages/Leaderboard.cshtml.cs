using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Points { get; set; }
}

public class Leaderboard(PioneerService _pioneerService) : PageModel
{
    public List<LeaderboardEntry> LeaderboardEntries { get; set; } = new();
    
    public async Task OnGet()
    {
        var profiles = await _pioneerService.GetProfiles();

        LeaderboardEntries = profiles
            .Where(p => p.Points > 0)
            .OrderByDescending(p => p.Points)
            .GroupBy(p => p.Points)
            .SelectMany((r, i) => r.Select(p => new LeaderboardEntry
                {
                    Rank = i + 1,
                    UserId = p.Id,
                    Name = p.GetDisplayName(),
                    Points = p.Points
                }))
            .ToList();
    }
}