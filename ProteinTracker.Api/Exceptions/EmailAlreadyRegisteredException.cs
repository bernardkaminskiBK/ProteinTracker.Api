namespace ProteinTracker.Api.Exceptions;

public class EmailAlreadyRegisteredException
    : Exception
{
    public EmailAlreadyRegisteredException()
        : base("An account with this email address already exists.")
    {
    }
}
