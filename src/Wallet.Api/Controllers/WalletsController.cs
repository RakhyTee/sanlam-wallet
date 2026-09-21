using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Extensions;
using Wallet.Api.Mappers;
using Wallet.Api.Models.Requests;
using Wallet.Api.Models.Responses;
using Wallet.Application.Commands.Withdraw;
using Wallet.Application.Queries.Balance;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/wallets")]
public class WalletsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly IMediator _mediator;

    public WalletsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}/balance", Name = "GetWalletBalance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBalanceQuery { WalletId = id }, cancellationToken);

        if (!result.IsSuccess)
        {
            var (statusCode, title) = result.Error!.ToProblemInfo();
            return Problem(title: title, statusCode: statusCode);
        }

        return Ok(WalletResponseMapper.ToResponse(result.Value!));
    }

    [HttpPost("{id:guid}/withdrawals", Name = "WithdrawFunds")]
    [ProducesResponseType(typeof(WithdrawalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(
        Guid id,
        [FromBody] WithdrawRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new WithdrawCommand
        {
            WalletId = id,
            Amount = request.Amount,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var (statusCode, title) = result.Error!.ToProblemInfo();
            return Problem(title: title, statusCode: statusCode);
        }

        var response = WalletResponseMapper.ToResponse(result.Value!);
        return Created($"/api/wallets/{id}/withdrawals/{response.WithdrawalId}", response);
    }
}
