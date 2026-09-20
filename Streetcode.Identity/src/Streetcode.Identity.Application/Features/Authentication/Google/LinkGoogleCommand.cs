using FluentResults;
using FluentValidation;
using MediatR;
using Streetcode.Identity.Application.Abstractions;

namespace Streetcode.Identity.Application.Features.Authentication.Google;

public sealed record LinkGoogleCommand(string IdToken, Guid UserId, long AccessVersion,
    string Password, bool ConfirmLink) : IRequest<Result>;

public sealed class LinkGoogleCommandValidator : AbstractValidator<LinkGoogleCommand>
{
    public LinkGoogleCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty().MaximumLength(16384);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AccessVersion).GreaterThan(0);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ConfirmLink).Equal(true).WithMessage("Explicit confirmation is required to link Google.");
    }
}

public sealed class LinkGoogleCommandHandler(IGoogleTokenVerifier verifier, IGoogleIdentityService identity)
    : IRequestHandler<LinkGoogleCommand, Result>
{
    public async Task<Result> Handle(LinkGoogleCommand request, CancellationToken cancellationToken)
    {
        var verification = await verifier.VerifyAsync(request.IdToken, cancellationToken);
        if (verification.IsFailed) return Result.Fail(verification.Errors);

        return await identity.LinkAsync(verification.Value, request.UserId, request.AccessVersion,
            request.Password, cancellationToken);
    }
}
