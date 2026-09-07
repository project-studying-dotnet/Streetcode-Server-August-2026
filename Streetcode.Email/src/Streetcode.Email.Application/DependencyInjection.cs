using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Email.Application.EmailRequests;

namespace Streetcode.Email.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<
            RequestEmailDeliveryCommandValidator>();

        return services;
    }
}
