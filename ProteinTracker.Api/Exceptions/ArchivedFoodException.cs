namespace ProteinTracker.Api.Exceptions;

public class ArchivedFoodException(int foodId)
    : Exception($"Archived food with id {foodId} cannot be used for a new or reassigned food entry.");
