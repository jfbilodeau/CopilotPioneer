using Microsoft.AspNetCore.Components;

namespace CopilotPioneerWeb.Components.Pages;

public partial class SubmitPrompt : ComponentBase
{
    private SubmitPromptModel Model { get; set; } = new SubmitPromptModel();
    
    private void OnSubmit()
    {
        throw new NotImplementedException();
    }
}

sealed class SubmitPromptModel
{
    public string Author { get; set; } = "";
    public string Product { get; set; } = "";
    public string Title { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Notes { get; set; } = "";
    public byte[][] Images { get; set; } = new byte[3][];
}
