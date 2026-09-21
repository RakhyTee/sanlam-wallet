namespace Wallet.Domain.Common;

public class Result
{
    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; set; }

    public static Result Ok() => new() { IsSuccess = true };
 
    public static Result Fail(Error error) => new() { IsSuccess = false, Error = error };
 
    public static Result<T> Ok<T>(T value) => new() { IsSuccess = true, Value = value };
 
    public static Result<T> Fail<T>(Error error) => new() { IsSuccess = false, Error = error };
}

public sealed class Result<T> : Result
{
    public T? Value { get; set; }
}