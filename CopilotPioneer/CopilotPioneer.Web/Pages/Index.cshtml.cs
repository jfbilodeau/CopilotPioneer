using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class IndexModel(ILogger<IndexModel> logger, PioneerService pioneerService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly PioneerService _pioneerService = pioneerService;

    [FromQuery(Name = "page")]
    public int PageNumber { get; set; } = 0;

    // [FromQuery(Name = "count")]
    public int PageSize { get; set; } = 10;

    public List<Submission> Submissions { get; private set; } = new List<Submission>();

    public async Task OnGet()
    {
        Submissions = await _pioneerService.GetLatestSubmissions(PageNumber, PageSize);
    }
}