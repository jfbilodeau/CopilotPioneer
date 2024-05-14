using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class IndexModel(ILogger<IndexModel> logger, PioneerService pioneerService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public PioneerService PioneerService { get; private set; } = pioneerService;
    
    [FromQuery(Name = "page")]
    public int PageNumber { get; set; } = 0;

    [BindProperty(SupportsGet = true)]
    public string ProductFilter { get; set; } = "";
    [BindProperty(SupportsGet = true)]
    public string TagFilter { get; set; } = "";
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "latest";
    
    // [FromQuery(Name = "count")]
    public int PageSize { get; set; } = 10;

    public List<Submission> Submissions { get; private set; } = new();

    public async Task OnGet()
    {
        // Submissions = await PioneerService.GetLatestSubmissions(PageNumber, PageSize);
        Submissions = await PioneerService.GetSubmissionsByFilter(ProductFilter, TagFilter, SortBy, PageNumber, PageSize);
    }
}