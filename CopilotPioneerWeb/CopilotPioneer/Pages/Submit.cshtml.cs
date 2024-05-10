using CopilotPioneerWeb;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Pages;

public class Submit : PageModel
{
    public PioneerService PioneerService { get; private set; }
    
    public Submission Submission { get; } = new Submission();

    public Submit(PioneerService pioneerService)
    {
        PioneerService = pioneerService;
    }
    
    public void OnGet()
    {
        
    }

    public void OnPost(Submission submission)
    {
        
    }
}