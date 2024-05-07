using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace CopilotPioneerWeb.Components.Pages;

public partial class SubmitPrompt : ComponentBase
{
    [Inject]
    private PioneerService PioneerService { get; set; } = default!;
    
    [SupplyParameterFromForm]
    private SubmitPromptModel Model { get; set;  } = new();

    private void OnSubmit()
    {
        throw new NotImplementedException();
    }
}

sealed class SubmitPromptModel
{
    public string Author { get; set; } = "";
    public string Product { get; set; } = "";
    
    [Required]
    public string Title { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Notes { get; set; } = "";
    public byte[][] Images { get; set; } = new byte[3][];
}
