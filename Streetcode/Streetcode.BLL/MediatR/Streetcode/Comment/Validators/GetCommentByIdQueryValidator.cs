using FluentValidation;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.Validators;

public sealed class GetCommentByIdQueryValidator : AbstractValidator<GetCommentByIdQuery>
{
    public GetCommentByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .MustBeValidId("Comment");
    }
}
