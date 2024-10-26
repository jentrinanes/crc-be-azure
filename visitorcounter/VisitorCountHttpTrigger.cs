using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

namespace Crc.Function
{
    public class VisitorCountHttpTrigger
    {
        private readonly ILogger<VisitorCountHttpTrigger> _logger;

        public VisitorCountHttpTrigger(ILogger<VisitorCountHttpTrigger> logger)
        {
            _logger = logger;
        }

        [Function("VisitorCountHttpTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext executionContext)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var visitorCount = await GetVisitorCountAsync(executionContext);
            return new OkObjectResult(visitorCount);
        }
        
        private async Task<VisitorCount> GetVisitorCountAsync(FunctionContext executionContext)
        {            
            // test comment
            
            var keyVaultClient = GetSecretClient();
            var cosmosDbUri = keyVaultClient.GetSecret("CosmosDbUri");
            var cosmosDbPrimaryKey = keyVaultClient.GetSecret("CosmosDbPrimaryKey");
            
            var cosmosClient = new CosmosClient(
                cosmosDbUri.Value.Value, 
                cosmosDbPrimaryKey.Value.Value                
            );

            var container = cosmosClient.GetContainer("VisitorCountDb", "VisitorCountContainer");
            var query = container.GetItemLinqQueryable<VisitorCount>(true);

            var visitorCount = query.Where(x => x.id == "1").AsEnumerable().FirstOrDefault();
            if (visitorCount == null) {
                visitorCount = new VisitorCount { id = "1", count = 1 };
            }
            
            visitorCount.count++;
            await container.UpsertItemAsync(visitorCount);

            return visitorCount;            
        }

        private SecretClient GetSecretClient() {
            return new SecretClient(new Uri("https://keyvaultcrcjen.vault.azure.net/"), 
                new DefaultAzureCredential());
        } 
    }

    public class VisitorCount 
    {
        public string id { get; set; }
        public int count { get; set; }
    }   
}
