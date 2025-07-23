using Application.Commands.CreateTransaction;
using Application.Queries.GetTransactionById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/[controller]")]
/// <summary>
/// API controller for handling transactions.
/// </summary>
public class TransactionController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance for handling commands and queries.</param>
    public TransactionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="command">The command containing the transaction details.</param>
    /// <returns>A 201 Created response with the location of the new resource and the created transaction details.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTransactionById), new { id = result.TransactionExternalId }, result);
    }

    /// <summary>
    /// Retrieves a transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The external unique identifier of the transaction.</param>
    /// <returns>A 200 OK response with the transaction data if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var query = new GetTransactionByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
