using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class Tags(PioneerService pioneerService) : PageModel
{
    public List<Submission> Submissions { get; private set; } = new();
    
    public void OnGet()
    {
        
    }
}