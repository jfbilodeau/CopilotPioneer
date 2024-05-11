using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace CopilotPioneerWeb.Components.Pages;

public partial class SubmitPrompt : ComponentBase
{
    [Inject]
    private PioneerService PioneerService { get; set; } = default!;
    
    [SupplyParameterFromForm]
    private Submission Model { get; set;  } = new();

    private void OnSubmit()
    {
        throw new NotImplementedException();
    }
}
