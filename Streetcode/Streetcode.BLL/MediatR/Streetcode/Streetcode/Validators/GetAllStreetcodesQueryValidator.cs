using FluentValidation;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetAll;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetAllStreetcodesQueryValidator
    : AbstractValidator<GetAllStreetcodesQuery>
{
    public GetAllStreetcodesQueryValidator(
        IValidator<GetAllStreetcodesRequestDTO> requestValidator)
    {
        RuleFor(query => query.request)
            .NotNull()
            .WithName("Request")
            .WithMessage(ErrorMessages.Field_Required)
            .SetValidator(requestValidator);
    }
}