using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CoffeeNChillGateway.Middleware;
using CoffeeNChillGateway.Options;
using CoffeeNChillGateway.Services;
using CoffeeNChillGateway.Swagger;
using Microsoft.AspNetCore.Identity;
// using CoffeeNChillGateway.Auth;   // Member 2: uncomment when their PR is merged

var builder = WebApplication.CreateBuilder(args);

// ---- Storage clients ----
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
var storage = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

builder.Services.AddSingleton(new TableServiceClient(storage.ConnectionString));
builder.Services.AddSingleton(new BlobServiceClient(storage.ConnectionString));
builder.Services.AddSingleton(new QueueServiceClient(
    storage.ConnectionString,
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Services.AddHostedService<StorageInitializer>();

// builder.Services.AddSingleton<IMenuService, MenuService>();         // enable in Step 7
// builder.Services.AddSingleton<IOrderService, OrderService>();       // enable in Step 8
// builder.Services.AddSingleton<IDocumentService, DocumentService>(); // enable in Step 9

// builder.Services.AddCoffeeAuth(builder.Configuration);              // Member 2

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCoffeeSwagger();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoffeeNChill Gateway v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "CoffeeNChill Gateway";
});

// app.UseAuthentication();   // Member 2
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", utc = DateTime.UtcNow })).AllowAnonymous();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();