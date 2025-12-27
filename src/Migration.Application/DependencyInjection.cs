using Microsoft.Extensions.DependencyInjection;
using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Connections.Commands;
using DMS.Migration.Application.Connections.Queries;
using DMS.Migration.Application.Discovery.Commands;

namespace DMS.Migration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ============================================================
        // USE CASE HANDLERS - Connection Module
        // ============================================================
        services.AddScoped<ICommandHandler<CreateConnectionCommand, Connections.DTOs.ConnectionDto>,
            CreateConnectionCommandHandler>();
        services.AddScoped<ICommandHandler<VerifyConnectionCommand, Connections.DTOs.VerificationResultDto>,
            VerifyConnectionCommandHandler>();
        services.AddScoped<IQueryHandler<GetConnectionByIdQuery, Connections.DTOs.ConnectionDto>,
            GetConnectionByIdQueryHandler>();

        // ============================================================
        // USE CASE HANDLERS - Discovery Module
        // ============================================================
        services.AddScoped<ICommandHandler<CreateDiscoveryRunCommand, Discovery.DTOs.DiscoveryRunDto>,
            CreateDiscoveryRunCommandHandler>();

        // ============================================================
        // VALIDATORS (if using FluentValidation - optional)
        // ============================================================
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
