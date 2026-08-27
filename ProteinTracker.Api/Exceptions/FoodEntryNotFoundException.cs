namespace ProteinTracker.Api.Exceptions;

public class FoodEntryNotFoundException(int foodEntryId)
    : Exception($"Food entry with id {foodEntryId} was not found.");
