using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetByStreetcodeId;

namespace Streetcode.BLL.MediatR.Transactions.TransactionLink.Validators;

public sealed class GetTransactLinkByStreetcodeIdQueryValidator
    : AbstractValidator<GetTransactLinkByStreetcodeIdQuery>
{
    public GetTransactLinkByStreetcodeIdQueryValidator()
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");
    }
}