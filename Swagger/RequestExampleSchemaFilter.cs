using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using ProteinTracker.Api.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProteinTracker.Api.Swagger;

public class RequestExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Example = context.Type switch
        {
            Type type when type == typeof(CreateFoodRequest) => FoodExample("Oats", 7m, 77m, 1m),
            Type type when type == typeof(UpdateFoodRequest) => FoodExample("Rolled Oats", 8m, 68m, 7m),
            Type type when type == typeof(CreateFoodEntryRequest) => FoodEntryExample(
                1,
                150m,
                "2026-08-26T12:30:00+02:00"),
            Type type when type == typeof(UpdateFoodEntryRequest) => FoodEntryExample(
                2,
                175m,
                "2026-08-26T18:15:00+02:00"),
            Type type when type == typeof(UpdateDailyTargetRequest) => new OpenApiObject
            {
                ["proteinTarget"] = new OpenApiDouble(160),
                ["carbohydratesTarget"] = new OpenApiDouble(240),
                ["fatTarget"] = new OpenApiDouble(90)
            },
            _ => schema.Example
        };
    }

    private static OpenApiObject FoodExample(
        string name,
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        return new OpenApiObject
        {
            ["name"] = new OpenApiString(name),
            ["proteinPer100g"] = new OpenApiDouble((double)protein),
            ["carbohydratesPer100g"] = new OpenApiDouble((double)carbohydrates),
            ["fatPer100g"] = new OpenApiDouble((double)fat)
        };
    }

    private static OpenApiObject FoodEntryExample(
        int foodId,
        decimal amountInGrams,
        string consumedAt)
    {
        return new OpenApiObject
        {
            ["foodId"] = new OpenApiInteger(foodId),
            ["amountInGrams"] = new OpenApiDouble((double)amountInGrams),
            ["consumedAt"] = new OpenApiString(consumedAt)
        };
    }
}
