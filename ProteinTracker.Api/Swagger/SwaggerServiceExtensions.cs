using Microsoft.OpenApi.Models;

namespace ProteinTracker.Api.Swagger;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddProteinTrackerSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Protein Tracker API",
                Version = "v1",
                Description = "An authenticated backend for managing each user's reusable food definitions, " +
                    "daily intake, macronutrient targets, and nutrition summaries. Calories are derived " +
                    "from macros using the 4/4/9 rule."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter the JWT returned by register or login."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });

            options.OperationFilter<ProteinTrackerOperationFilter>();
            options.SchemaFilter<RequestExampleSchemaFilter>();
        });

        return services;
    }
}
