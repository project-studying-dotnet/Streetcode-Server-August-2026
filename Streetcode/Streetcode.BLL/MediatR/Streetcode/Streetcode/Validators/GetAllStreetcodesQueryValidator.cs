using FluentValidation;
using Streetcode.BLL.DTO.Streetcode;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetAll;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Validators;

public sealed class GetAllStreetcodesQueryValidator
    : AbstractValidator<GetAllStreetcodesQuery>
{
    public GetAllStreetcodesQueryValidator(
        IValidator<GetAllStreetcodesRequestDTO> requestValidator)
    {
        RuleFor(query => query.request)
            .NotNull()
            .WithMessage("Request cannot be null.")
            .SetValidator(requestValidator);
    }
}