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
    private readonly Container _submissionsContainer;
    
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _screenshotContainerClient;
    
    public PioneerService(IConfiguration configuration)
    {
        var cosmosDbConnectionString = configuration["CosmosDb:ConnectionString"];
        var cosmosDbDatabaseName = configuration["CosmosDb:DatabaseName"];
        
        var cosmosClient = new CosmosClient(cosmosDbConnectionString);
        _cosmosDbDatabase = cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbDatabaseName).Result;
        _cosmosDbDatabase = cosmosClient.GetDatabase(cosmosDbDatabaseName);
        _submissionsContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Submissions", "/id").Result;
        
        var blobStorageAccountName = configuration["BlobStorage:AccountName"];
        var blobStorageAccountKey = configuration["BlobStorage:AccountKey"];

        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={blobStorageAccountName};AccountKey={blobStorageAccountKey};EndpointSuffix=core.windows.net";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _screenshotContainerClient = _blobServiceClient.GetBlobContainerClient("screenshots");
    }
    
    public async Task<Submission> SaveSubmission(Submission submission)
    {
        submission.Id = Guid.NewGuid().ToString();
        submission.CreatedDate = DateTime.Now;
        submission.LastModifiedDate = DateTime.Now;
        
        var container = _cosmosDbDatabase.GetContainer("Submissions");
        await container.CreateItemAsync(submission, new PartitionKey(submission.Id));

        return submission;
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

    public async Task<Submission> GetSubmissionById(string submissionId)
    {
        throw new NotImplementedException();
    }
}