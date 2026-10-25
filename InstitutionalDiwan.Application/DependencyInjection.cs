using FluentValidation;
using InstitutionalDiwan.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace InstitutionalDiwan.Application;

public static class DependencyInjection
{
    /// <summary>Registers MediatR handlers, FluentValidation validators, and the pipeline behaviors.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // Cross-cutting rules shared by the movement and attachment command handlers.
        services.AddScoped<Features.Movements.CentralDiwanRouting>();
        services.AddScoped<Features.Attachments.AttachmentUploader>();

        return services;
    }
}
