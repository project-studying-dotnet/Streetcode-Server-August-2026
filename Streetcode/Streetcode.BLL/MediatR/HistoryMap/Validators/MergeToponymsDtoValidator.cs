using FluentValidation;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.HistoryMap.Validators
{
    public sealed class MergeToponymsDtoValidator
        : AbstractValidator<MergeToponymsDTO>
    {
        public MergeToponymsDtoValidator()
        {
            RuleFor(x => x.SourceToponymId)
                .MustBeValidId("Source toponym");

            RuleFor(x => x.TargetToponymId)
                .MustBeValidId("Target toponym");

            RuleFor(x => x.SourceToponymId)
                .NotEqual(x => x.TargetToponymId)
                .WithMessage("Source and target toponyms cannot be the same.");
        }
    }
}