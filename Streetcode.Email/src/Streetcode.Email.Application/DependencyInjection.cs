using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Email.Application.EmailRequests;
using Streetcode.Email.Application.EmailSending;

namespace Streetcode.Email.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<
            RequestEmailDeliveryCommandValidator>();

        services.AddScoped<RequestEmailDeliveryCommandHandler>();
        services.AddScoped<SendEmailDeliveryHandler>();
        services.AddScoped<MarkEmailDeliveryAsFailedHandler>();

        return services;
    }
}
