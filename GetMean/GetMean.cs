using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using System.ComponentModel;
using Azure;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace GetMean
{
    public static class GetMean
    {
        private static readonly string cosmosDbEndpoint = Environment.GetEnvironmentVariable("DBENDPOINT", EnvironmentVariableTarget.Process);
        private static readonly string cosmosDbKey = Environment.GetEnvironmentVariable("DBKEY", EnvironmentVariableTarget.Process);
        private static readonly string databaseId = Environment.GetEnvironmentVariable("DBID", EnvironmentVariableTarget.Process);
        private static readonly string containerId = Environment.GetEnvironmentVariable("CONTAINERID", EnvironmentVariableTarget.Process);
        private static readonly CosmosClient cosmosClient = new CosmosClient(cosmosDbEndpoint, cosmosDbKey);
        private static readonly Microsoft.Azure.Cosmos.Container cosmosContainer = cosmosClient.GetContainer(databaseId, containerId);

        [FunctionName("GetMean")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            /*
            // faster version
            try
            {
                string country = req.Query["Country"];

                // Validate that the country is provided  
                if (string.IsNullOrEmpty(country))
                {
                    return new BadRequestObjectResult("The 'Country' query parameter is required.");
                }

                // Create a SQL query to retrieve the volcano entities with the same country  
                var sqlQueryText = $"SELECT * FROM c WHERE c.Country = '{country}'";
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<Volcano> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<Volcano>(queryDefinition);

                List<Volcano> volcanoes = new List<Volcano>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<Volcano> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (Volcano volcano in currentResultSet)
                    {
                        volcanoes.Add(volcano);
                    }
                }

                // If no volcanoes found, return not found  
                if (!volcanoes.Any())
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(volcanoes);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while retrieving data from Cosmos DB.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
            */
            // slower version
            ///*
            try
            {
                string country = req.Query["Country"];

                // Validate that the country is provided  
                if (string.IsNullOrEmpty(country))
                {
                    return new BadRequestObjectResult("The 'Country' query parameter is required.");
                }

                FeedIterator<Volcano> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<Volcano>();
                //List<int> elevations = new List<int>();
                double sum = 0;
                int count = 0;

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<Volcano> currentResultSet = await queryResultSetIterator.ReadNextAsync();

                    foreach (Volcano volcano in currentResultSet)
                    {
                        if (volcano.Country == country)
                        {
                            if (int.TryParse(volcano.Elevation, out int parsedElevation))
                            {
                                sum += parsedElevation;
                                count++;
                            }
                            else
                            {
                                log.LogWarning($"Could not parse elevation '{volcano.Elevation}' for volcano '{volcano.VolcanoName}'. This elevation will be ignored.");
                            }
                        }
                    }
                }


                // If no volcanoes found, return not found  
                if (count == 0)
                {
                    return new NotFoundResult();
                }

                // Calculate the mean elevation  
                double meanElevation = Math.Round(sum / count, 3);


                return new OkObjectResult(meanElevation);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while retrieving data from Cosmos DB.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
        //*/
    }
    public class Volcano
    {
        public string VolcanoName { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public Location Location { get; set; }
        public string Elevation { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string LastKnownEruption { get; set; }
        public string Id { get; set; }
    }

    public class Location
    {
        public string Type { get; set; }
        public List<double> Coordinates { get; set; }
    }
}
