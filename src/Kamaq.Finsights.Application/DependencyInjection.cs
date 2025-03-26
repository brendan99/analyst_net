using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MediatR;

namespace Kamaq.Finsights.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            
        // AutoMapper will be added later when dependency issues are resolved
        // services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        
        // Add FluentValidation
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Add MediatR behaviors (pipeline)
        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        
        return services;
    }
} 