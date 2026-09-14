using FluentValidation;
using Streetcode.BLL.DTO.Toponyms;
using Streetcode.BLL.MediatR.Toponyms.GetAll;

namespace Streetcode.BLL.MediatR.Toponyms.Validators;

public sealed class GetAllToponymsQueryValidator
    : AbstractValidator<GetAllToponymsQuery>
{
    public GetAllToponymsQueryValidator(
        IValidator<GetAllToponymsRequestDTO> requestValidator)
    {
        RuleFor(query => query.request)
            .NotNull()
            .WithMessage("Request is required.")
            .SetValidator(requestValidator);
    }
}
