using FluentValidation;
using Streetcode.BLL.DTO.Toponyms;
using Streetcode.BLL.MediatR.Toponyms.GetAll;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Toponyms.Validators;

public sealed class GetAllToponymsQueryValidator
    : AbstractValidator<GetAllToponymsQuery>
{
    public GetAllToponymsQueryValidator(
        IValidator<GetAllToponymsRequestDTO> requestValidator)
    {
        RuleFor(query => query.request)
            .NotNull()
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(requestValidator);
    }
}
