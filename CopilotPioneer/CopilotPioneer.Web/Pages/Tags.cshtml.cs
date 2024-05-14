using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class Tags(PioneerService pioneerService) : PageModel
{
    [BindProperty]
    public string Tag { get; set; } = "";
    
    [FromQuery(Name = "page")]
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public List<Submission> Submissions { get; private set; } = [];
    
    public void OnGet()
    {
        // Submissions = pioneerService.GetSubmissions();
    }
}