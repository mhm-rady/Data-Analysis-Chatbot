using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using SsasMcpServer.Services;
using SsasMcpServer.Services.Interfaces;
using SsasMcpServer.Models.Query;
using SsasMcpServer.Core.Mocks;

// Build a generic host to use the same DI and configuration stack used by the project
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        // keep default config sources (appsettings.json, env, args)
    })
    .ConfigureServices((ctx, services) =>
    {
        // Register the real SSAS-related services
        services.AddSsasServices(ctx.Configuration);

        // For a runnable demo without an actual SSAS server, override IQueryService with a mock
        //services.AddSingleton<IQueryService, MockQueryService>();
    })
    .Build();

// Resolve the query service and run the sample request
var queryService = host.Services.GetRequiredService<IQueryService>();

var queryRequest = new QueryRequest
{
    Columns = new List<string> { "'Product Category'[Product Category Name]", "'Product'[Product Name]" },
    Measures = new List<string> { "[Internet Total Sales]" },
    Filters = new List<QueryFilter>
    {
        new() { Column = "'Date'[Calendar Year]", Operator = FilterOperator.Equals, Value = 2014 }
    },
    Sorts = new List<QuerySort>
    {
        new() { Column = "[Internet Total Sales]", Direction = SortDirection.Descending }
    },
    MaxRows = 100,
    Culture = "en-US"
};

var result = await queryService.ExecuteQueryAsync(queryRequest);

// Print result as JSON
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);

// Dispose host cleanly
await host.StopAsync();

