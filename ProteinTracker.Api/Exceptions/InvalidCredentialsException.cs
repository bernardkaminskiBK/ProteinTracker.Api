namespace ProteinTracker.Api.Exceptions;

public class InvalidCredentialsException
    : Exception
{
    public InvalidCredentialsException()
        : base("The email or password is incorrect.")
    {
    }
}
