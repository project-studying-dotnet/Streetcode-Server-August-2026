using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetById;

namespace Streetcode.BLL.MediatR.Transactions.TransactionLink.Validators;

public sealed class GetTransactLinkByIdQueryValidator
    : AbstractValidator<GetTransactLinkByIdQuery>
{
    public GetTransactLinkByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Transaction link");
    }
}