using FluentValidation;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetById;

namespace Streetcode.BLL.MediatR.Transactions.TransactionLink.Validators;

public sealed class GetTransactLinkByIdQueryValidator
    : AbstractValidator<GetTransactLinkByIdQuery>
{
    public GetTransactLinkByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Transaction link ID must be greater than 0.");
    }
}