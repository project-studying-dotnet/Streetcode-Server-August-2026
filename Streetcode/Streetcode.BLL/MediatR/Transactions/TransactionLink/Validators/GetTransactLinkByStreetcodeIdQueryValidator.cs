using FluentValidation;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Transactions.TransactionLink.Validators;

public sealed class GetTransactLinkByStreetcodeIdQueryValidator
    : AbstractValidator<GetTransactLinkByStreetcodeIdQuery>
{
    public GetTransactLinkByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .GreaterThan(0)
            .WithMessage("Streetcode ID must be greater than 0.");
    }
}