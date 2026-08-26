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
                Description = "A single-user backend for managing reusable food definitions, " +
                    "tracking daily intake, configuring macronutrient targets, and viewing daily " +
                    "nutrition summaries. Calories are derived from macros using the 4/4/9 rule."
            });

            options.OperationFilter<ProteinTrackerOperationFilter>();
            options.SchemaFilter<RequestExampleSchemaFilter>();
        });

        return services;
    }
}
