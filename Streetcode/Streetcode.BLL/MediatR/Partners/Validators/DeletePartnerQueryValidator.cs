using FluentValidation;
using Streetcode.BLL.MediatR.Validators;
using Streetcode.BLL.MediatR.Partners.Delete;

namespace Streetcode.BLL.MediatR.Partners.Validators;

public sealed class DeletePartnerQueryValidator
    : AbstractValidator<DeletePartnerQuery>
{
    public DeletePartnerQueryValidator()
    {
        RuleFor(query => query.id)
            .MustBeValidId("Partner");
    }
}