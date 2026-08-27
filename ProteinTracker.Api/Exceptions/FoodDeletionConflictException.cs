namespace ProteinTracker.Api.Exceptions;

public class FoodDeletionConflictException(int foodId)
    : Exception($"Food with id {foodId} cannot be permanently deleted because historical food entries reference it.");
