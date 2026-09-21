using FluentValidation.Results;
using Wallet.Domain.Common;
using Wallet.Domain.Wallets;

namespace Wallet.Api.Extensions;

public static class ErrorExtensions
{
    public static (int StatusCode, string Title) ToProblemInfo(this Error error) => error.Code switch
    {
        var code when code == WalletErrors.NotFound.Code => (StatusCodes.Status404NotFound, error.Message),
        var code when code == WalletErrors.InsufficientFunds.Code => (StatusCodes.Status422UnprocessableEntity, error.Message),
        var code when code == WalletErrors.InvalidAmount.Code => (StatusCodes.Status400BadRequest, error.Message),
        var code when code == WalletErrors.ConcurrencyConflict.Code => (StatusCodes.Status409Conflict, error.Message),
        _ => (StatusCodes.Status500InternalServerError, error.Message)
    };

    public static IResult ToProblem(this Error error)
    {
        var (statusCode, title) = error.ToProblemInfo();
        return Results.Problem(title: title, statusCode: statusCode);
    }

    public static IResult ToProblem(this IEnumerable<ValidationFailure> failures)
    {
        var first = failures.First();

        if (first.ErrorCode == WalletErrors.NotFound.Code || first.ErrorCode == WalletErrors.InsufficientFunds.Code)
            return new Error(first.ErrorCode, first.ErrorMessage).ToProblem();

        return Results.ValidationProblem(failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()));
    }
}
