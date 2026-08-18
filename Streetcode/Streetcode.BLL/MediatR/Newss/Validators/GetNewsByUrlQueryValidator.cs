using FluentValidation;
using Streetcode.BLL.MediatR.Newss.GetByUrl;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsByUrlQueryValidator
    : AbstractValidator<GetNewsByUrlQuery>
{
    public GetNewsByUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithMessage("URL is required.")
            .MaximumLength(100)
            .WithMessage("URL must not exceed 100 characters.");
    }
}