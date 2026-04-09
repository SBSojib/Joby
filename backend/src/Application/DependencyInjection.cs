using FluentValidation;
using Joby.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Joby.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IJobService>();
        return services;
    }
}


