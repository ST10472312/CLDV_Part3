using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CoffeeNChillGateway.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCoffeeSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CoffeeNChill Gateway API",
                Version = "v1",
                Description = "Unified API for the CoffeeNChill canteen: menu, order queue, staff documents and JWT authentication."
            });

            // Pull summaries, <param> and <response> text from the generated XML file.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            // The "Authorize" button in Swagger UI. Users paste the token only (no "Bearer " prefix).
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT returned by POST /api/auth/login."
            });

            // Shows the padlock only on endpoints that really require a token.
            options.OperationFilter<AuthorizeOperationFilter>();
        });
        return services;
    }
}

/// <summary>Adds the padlock plus 401/403 responses to protected operations only.</summary>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any();
        if (!requiresAuth || allowsAnonymous) return;

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid token" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Signed in, but your role is not allowed" });

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new() { [scheme] = Array.Empty<string>() }
        };
    }
}