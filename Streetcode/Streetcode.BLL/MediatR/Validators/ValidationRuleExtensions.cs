using FluentValidation;

namespace Streetcode.BLL.MediatR.Validators;

public static class ValidationRuleExtensions
{
    public static IRuleBuilderOptions<T, int> MustBeValidId<T>(
        this IRuleBuilder<T, int> ruleBuilder,
        string resourceName)
    {
        return ruleBuilder
            .GreaterThan(0)
            .WithMessage($"{resourceName} ID must be greater than 0.");
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
            .WithMessage(
                $"{fieldName} must be a valid HTTP or HTTPS URL.");
    }

    public static IRuleBuilderOptions<T, string?> MustNotExceedLength<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maximumLength,
        string fieldName)
    {
        return ruleBuilder
            .MaximumLength(maximumLength)
            .WithMessage(
                $"{fieldName} must not exceed {maximumLength} characters.");
    }
}
