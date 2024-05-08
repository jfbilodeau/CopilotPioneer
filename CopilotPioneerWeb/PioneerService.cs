using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;

namespace CopilotPioneerWeb;

public class Product
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
}

public class PioneerService
{
    private readonly Database _cosmosDbDatabase;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _screenshotContainerClient;
    
    public PioneerService(IConfiguration configuration)
    {
        var cosmosDbConnectionString = configuration["CosmosDb:ConnectionString"];
        var cosmosDbDatabaseName = configuration["CosmosDb:DatabaseName"];
        
        var cosmosClient = new CosmosClient(cosmosDbConnectionString);
        _cosmosDbDatabase = cosmosClient.GetDatabase(cosmosDbDatabaseName);
        
        var blobStorageAccountName = configuration["BlobStorage:AccountName"];
        var blobStorageAccountKey = configuration["BlobStorage:AccountKey"];

        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={blobStorageAccountName};AccountKey={blobStorageAccountKey};EndpointSuffix=core.windows.net";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _screenshotContainerClient = _blobServiceClient.GetBlobContainerClient("screenshots");
    }
    
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