using Microsoft.AspNetCore.Components;

namespace CopilotPioneerWeb.Components.Pages;

public partial class Submissions : ComponentBase
{
    [Inject]
    private PioneerService PioneerService { get; set; } = default!;
    
    [Parameter]
    public string? SubmissionId { get; set; }

    public Submission? Submission { get; set; } = null;

    protected override async void OnInitialized()
    {
        if (SubmissionId is not null)
        {
            Submission = await PioneerService.GetSubmissionById(SubmissionId);
        }
    }
}