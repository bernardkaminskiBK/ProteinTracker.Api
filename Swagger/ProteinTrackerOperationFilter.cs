using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProteinTracker.Api.Swagger;

public class ProteinTrackerOperationFilter : IOperationFilter
{
    private static readonly IReadOnlyDictionary<(string Controller, string Action), OperationDocumentation>
        Documentation = CreateDocumentation();

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor action)
        {
            return;
        }

        if (!Documentation.TryGetValue((action.ControllerName, action.ActionName), out var documentation))
        {
            return;
        }

        operation.Summary = documentation.Summary;
        operation.Description = documentation.Description;

        if (action.ControllerName == "DailySummary" && action.ActionName == "Get")
        {
            var dateParameter = operation.Parameters
                .First(parameter => parameter.Name == "date");

            dateParameter.Description = "The local calendar day to summarize, in required yyyy-MM-dd " +
                "format (for example, 2026-08-26). The date is interpreted in the application's " +
                "configured Europe/Bratislava timezone.";
            dateParameter.Required = true;
            dateParameter.Example = new OpenApiString("2026-08-26");
            dateParameter.Schema.Format = "date";
            dateParameter.Schema.Example = new OpenApiString("2026-08-26");
        }

        foreach (var response in documentation.Responses)
        {
            operation.Responses[response.Key] = CreateResponse(
                response.Key,
                response.Value,
                context);
        }
    }

    private static OpenApiResponse CreateResponse(
        string statusCode,
        string description,
        OperationFilterContext context)
    {
        var response = new OpenApiResponse { Description = description };

        if (statusCode is "400" or "404" or "500")
        {
            response.Content["application/problem+json"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(
                    typeof(ProblemDetails),
                    context.SchemaRepository)
            };
        }

        return response;
    }

    private static IReadOnlyDictionary<(string, string), OperationDocumentation> CreateDocumentation()
    {
        return new Dictionary<(string, string), OperationDocumentation>
        {
            [("Foods", "GetAllActive")] = Doc(
                "List active foods",
                "Returns reusable nutritional definitions that are currently available for new entries. " +
                "Macro values are stored per 100g; calories are calculated with the 4/4/9 rule.",
                OkList()),
            [("Foods", "GetAllArchived")] = Doc(
                "List archived foods",
                "Returns soft-deleted food definitions. Archived foods remain available to historical " +
                "entries and can be restored, but cannot be selected for new entries.",
                OkList()),
            [("Foods", "GetById")] = Doc(
                "Get a food definition",
                "Returns the current per-100g macro values and calculated calories for one food, including " +
                "its archive state.",
                Responses(("200", "Food returned."), ("404", "Food not found."), ("500", "Unexpected server error."))),
            [("Foods", "Create")] = Doc(
                "Create a reusable food definition",
                "Creates an active food with non-negative per-100g macros. The name is required and trimmed. " +
                "Calories are calculated rather than stored.",
                Responses(("201", "Food created; Location identifies the new resource."), ("400", "Request violates a food business rule."), ("500", "Unexpected server error."))),
            [("Foods", "Update")] = Doc(
                "Update a food definition",
                "Replaces the editable name and per-100g macros. Historical entries immediately use these " +
                "current values when nutrition is recalculated; no nutritional snapshots are kept.",
                Responses(("200", "Food updated."), ("400", "Request violates a food business rule."), ("404", "Food not found."), ("500", "Unexpected server error."))),
            [("Foods", "Archive")] = Doc(
                "Archive a food",
                "Soft-deletes a food so it cannot be used for new entries. Existing historical entries retain " +
                "the relationship. Repeating this operation is safe.",
                Responses(("200", "Food is archived."), ("404", "Food not found."), ("500", "Unexpected server error."))),
            [("Foods", "Restore")] = Doc(
                "Restore an archived food",
                "Makes a food available for new entries again. Repeating this operation for an active food is safe.",
                Responses(("200", "Food is active."), ("404", "Food not found."), ("500", "Unexpected server error."))),

            [("FoodEntries", "GetById")] = Doc(
                "Get a food entry",
                "Returns an entry with nutrition calculated from its amount and the referenced Food's current " +
                "per-100g values.",
                Responses(("200", "Food entry returned."), ("404", "Food entry not found."), ("500", "Unexpected server error."))),
            [("FoodEntries", "GetByDateRange")] = Doc(
                "List food entries in a timestamp range",
                "Returns entries where start is inclusive and end is exclusive. Both DateTimeOffset boundaries " +
                "are used exactly as supplied; this endpoint performs no calendar-day or timezone interpretation.",
                OkList()),
            [("FoodEntries", "Create")] = Doc(
                "Record consumed food",
                "Creates an entry for a positive gram amount. The referenced food must exist and be active. " +
                "Nutrition is calculated from current Food values and is not stored on the entry. Clients may " +
                "submit an offset-aware timestamp; it is normalized to UTC when persisted.",
                Responses(("201", "Food entry created; Location identifies the new resource."), ("400", "Amount is invalid or the food is archived."), ("404", "Referenced food not found."), ("500", "Unexpected server error."))),
            [("FoodEntries", "Update")] = Doc(
                "Update a food entry",
                "Updates the food, amount, and timestamp. An archived historical food may remain assigned, " +
                "but reassignment requires an active food. Offset-aware timestamps are normalized to UTC when persisted.",
                Responses(("200", "Food entry updated."), ("400", "Amount is invalid or reassigned food is archived."), ("404", "Food entry or reassigned food not found."), ("500", "Unexpected server error."))),
            [("FoodEntries", "Delete")] = Doc(
                "Delete a food entry",
                "Physically removes an individual consumption record. Food definitions themselves use archiving instead.",
                Responses(("204", "Food entry deleted."), ("404", "Food entry not found."), ("500", "Unexpected server error."))),

            [("DailyTarget", "GetCurrent")] = Doc(
                "Get the current daily macro target",
                "Returns the single-user macro target and calculated calorie target. If no target has been " +
                "configured, all values are zero and no database record is created.",
                Responses(("200", "Current target returned."), ("500", "Unexpected server error."))),
            [("DailyTarget", "Update")] = Doc(
                "Set the current daily macro target",
                "Creates or updates the single current target. Macro values must be non-negative; calories use " +
                "the 4/4/9 rule and are never stored.",
                Responses(("200", "Current target updated."), ("400", "One or more target values are negative."), ("500", "Unexpected server error."))),

            [("DailySummary", "Get")] = Doc(
                "Get a daily nutrition summary",
                "Interprets the requested calendar date in Europe/Bratislava and returns consumed, target, and " +
                "remaining nutrition. Remaining equals target minus consumed; negative values mean the target " +
                "was exceeded. Current Food values and the current DailyTarget are used without snapshots.",
                Responses(("200", "Daily summary returned."), ("500", "Unexpected server error.")))
        };
    }

    private static OperationDocumentation Doc(
        string summary,
        string description,
        IReadOnlyDictionary<string, string> responses)
    {
        return new OperationDocumentation(summary, description, responses);
    }

    private static IReadOnlyDictionary<string, string> OkList()
    {
        return Responses(("200", "Matching resources returned; the collection may be empty."), ("500", "Unexpected server error."));
    }

    private static IReadOnlyDictionary<string, string> Responses(
        params (string StatusCode, string Description)[] responses)
    {
        return responses.ToDictionary(response => response.StatusCode, response => response.Description);
    }

    private sealed record OperationDocumentation(
        string Summary,
        string Description,
        IReadOnlyDictionary<string, string> Responses);
}
