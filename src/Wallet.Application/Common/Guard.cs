namespace Wallet.Application.Common;

public static class Guard
{

    public static void AgainstDefault(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", paramName);
    }
 
    public static void AgainstNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
    }
 
    public static void AgainstNegative(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
    }
}
