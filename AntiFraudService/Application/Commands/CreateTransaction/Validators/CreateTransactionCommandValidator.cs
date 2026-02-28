using Application.Commands.CreateTransaction;
using FluentValidation;

namespace Application.Commands.CreateTransaction.Validators;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required");

        RuleFor(x => x.TargetAccountId)
            .NotEmpty()
            .WithMessage("Target account ID is required");

        RuleFor(x => x.TransferTypeId)
            .GreaterThan(0)
            .WithMessage("Transfer type ID must be greater than 0");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("Transaction value must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Transaction value cannot exceed 1,000,000");
    }
}
