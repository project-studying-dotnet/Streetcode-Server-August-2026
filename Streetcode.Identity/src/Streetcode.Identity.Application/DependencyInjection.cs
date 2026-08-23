using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Common.Behaviors;
using Streetcode.Identity.Application.Features.Registration;

namespace Streetcode.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(
                typeof(DependencyInjection).Assembly);

            configuration.AddOpenBehavior(
                typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
