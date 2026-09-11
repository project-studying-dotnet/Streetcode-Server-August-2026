using FluentValidation;
using Streetcode.BLL.Resources;

namespace Streetcode.BLL.MediatR.Validators;

public static class ValidationRuleExtensions
{
    public static IRuleBuilderOptions<T, int> MustBeValidId<T>(
        this IRuleBuilder<T, int> ruleBuilder,
        string resourceName)
    {
        return ruleBuilder
            .GreaterThan(0)
            .WithMessage(string.Format(ErrorMessages.MustBeValidId, resourceName));
    }

    public static IRuleBuilderOptions<T, string?> MustBeValidHttpUrl<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        string fieldName)
    {
        return ruleBuilder
            .Must(url =>
                string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp ||
                  uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage(string.Format(ErrorMessages.MustBeValidHttpUrl, fieldName));
    }

    public static IRuleBuilderOptions<T, string?> MustNotExceedLength<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maximumLength,
        string fieldName)
    {
        return ruleBuilder
            .MaximumLength(maximumLength)
            .WithMessage(string.Format(ErrorMessages.MustNotExceedLength, fieldName, maximumLength));
    }
}
