using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Partners.GetById;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class GetPartnerByIdQueryValidator
    : AbstractValidator<GetPartnerByIdQuery>
{
    public GetPartnerByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Partner");
    }
}