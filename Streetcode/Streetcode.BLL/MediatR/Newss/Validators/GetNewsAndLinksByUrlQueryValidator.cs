using FluentValidation;
using Streetcode.BLL.MediatR.Newss.GetNewsAndLinksByUrl;

namespace Streetcode.BLL.MediatR.Newss.Validators;

public sealed class GetNewsAndLinksByUrlQueryValidator
    : AbstractValidator<GetNewsAndLinksByUrlQuery>
{
    public GetNewsAndLinksByUrlQueryValidator()
    {
        RuleFor(query => query.url)
            .NotEmpty()
            .WithMessage("URL is required.")
            .MaximumLength(100)
            .WithMessage("URL must not exceed 100 characters.");
    }
}