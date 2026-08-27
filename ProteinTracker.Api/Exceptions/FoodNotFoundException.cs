namespace ProteinTracker.Api.Exceptions;

public class FoodNotFoundException(int foodId)
    : Exception($"Food with id {foodId} was not found.");
