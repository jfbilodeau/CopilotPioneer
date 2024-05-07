namespace CopilotPioneerWeb;

public class Product
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
}

public class PioneerService
{
    public Product[] GetProducts()
    {
        return
        [
            new Product { Id = "M365", Label = "Microsoft 365" },
            new Product { Id = "Excel", Label = "Excel" },
            new Product { Id = "Loop", Label = "Loop" },
            new Product { Id = "PowerPoint", Label = "PowerPoint" },
            new Product { Id = "Teams", Label = "Teams" },
            new Product { Id = "Whiteboard", Label = "Whiteboard" },
            new Product { Id = "Word", Label = "Word" }
        ];
    }
}