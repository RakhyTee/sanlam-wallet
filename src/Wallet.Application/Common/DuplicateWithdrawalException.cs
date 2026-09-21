namespace Wallet.Application;

public class DuplicateWithdrawalException : Exception
{
    public DuplicateWithdrawalException(string message) : base(message)
    {
    }

    public DuplicateWithdrawalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
