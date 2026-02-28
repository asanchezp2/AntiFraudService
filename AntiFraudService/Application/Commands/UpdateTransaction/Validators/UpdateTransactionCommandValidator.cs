using Application.Commands.UpdateTransaction;
using FluentValidation;

namespace Application.Commands.UpdateTransaction.Validators;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("Transaction value must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Transaction value cannot exceed 1,000,000");
    }
}
