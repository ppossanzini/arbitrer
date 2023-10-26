using System;
using Arbitrer;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Arbitrer
{
  public static class DependecyInjectionExtension
  {
    public static IServiceCollection AddArbitrer(this IServiceCollection services, Action<ArbitrerOptions> configure = null)
    {
      if (configure != null)
        services.Configure<ArbitrerOptions>(configure);
      services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Pipelines.ArbitrerPipeline<,>));
      services.AddSingleton<IArbitrer, Arbitrer>();

      services.AddTransient<IMediator, ArbitredMediatr>();
      return services;
    }
  }
}