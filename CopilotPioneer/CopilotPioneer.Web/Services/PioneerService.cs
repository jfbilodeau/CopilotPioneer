using Azure.Storage.Blobs;
using CopilotPioneer.Web.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace CopilotPioneer.Web.Services;

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
        var cosmosDbConnectionString = configuration["CosmosDb.ConnectionString"];
        var cosmosDbDatabaseName = configuration["CosmosDb.DatabaseName"];

        var cosmosClient = new CosmosClientBuilder(cosmosDbConnectionString)
            .WithSerializerOptions(new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            })
            .Build();
        _cosmosDbDatabase = cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbDatabaseName).Result;
        _submissionsContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Submissions", "/author").Result;
        
        var blobStorageAccountName = configuration["BlobStorage.AccountName"];
        var blobStorageAccountKey = configuration["BlobStorage.AccountKey"];

        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={blobStorageAccountName};AccountKey={blobStorageAccountKey};EndpointSuffix=core.windows.net";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _screenshotContainerClient = _blobServiceClient.GetBlobContainerClient("screenshots");
    }
    
    public async Task<Submission> SaveSubmission(string submitter, Submission submission)
    {
        submission.Id = Guid.NewGuid().ToString();
        submission.CreatedDate = DateTime.Now;
        submission.LastModifiedDate = DateTime.Now;
        submission.Author = submitter;
        
        await _submissionsContainer.CreateItemAsync(submission);

        return submission;
    }

    public async Task<Submission?> GetSubmissionById(string submissionId)
    {
        var sql = "SELECT * FROM Submissions s where s.id = @submissionId";

        var query = new QueryDefinition(sql)
            .WithParameter("@submissionId", submissionId);

        var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        while (feedIterator.HasMoreResults)
        {
            var submissions = await feedIterator.ReadNextAsync();

            foreach (var submission in submissions)
            {
                return submission;
            }
        }

        return null;
    }
    
    public Product[] GetProducts()
    {
        // TODO -- pull those from DB
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
    
    public string GetProductName(string productId)
    {
        var products = GetProducts();
        var product = products.FirstOrDefault(p => p.Id == productId);

        return product?.Label ?? "[unknown product]";
    }
}