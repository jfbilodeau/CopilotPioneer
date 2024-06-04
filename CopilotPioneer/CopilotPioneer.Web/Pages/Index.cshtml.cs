using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class IndexModel(ILogger<IndexModel> logger, PioneerService pioneerService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public PioneerService PioneerService { get; private set; } = pioneerService;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 0;

    [BindProperty(SupportsGet = true)]
    public string UserIdFilter { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string ProductFilter { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string TagFilter { get; set; } = "";
    
    [BindProperty(SupportsGet = true)]
    public bool DailyWinner { get; set; } = false;
    
    [BindProperty(SupportsGet = true)]
    public bool WeeklyWinner { get; set; } = false;
    
    [BindProperty(SupportsGet = true)] 
    public string SortBy { get; set; } = "latest";

    public List<Submission> Submissions { get; private set; } = new();
    
    public bool HasFiltersApplied => !string.IsNullOrEmpty(UserIdFilter) || !string.IsNullOrEmpty(ProductFilter) || !string.IsNullOrEmpty(TagFilter) || DailyWinner || WeeklyWinner;

    public async Task OnGetAsync()
    {
        Submissions = await PioneerService.GetSubmissionsByFilter(UserIdFilter, ProductFilter, TagFilter, DailyWinner, WeeklyWinner, SortBy, PageNumber);
    }

    public ActionResult OnGetClearFilters()
    {
        // Redirect to index to clear query string (filters)
        return RedirectToPage();
    }
}