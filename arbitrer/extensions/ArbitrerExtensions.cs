
using System;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Arbitrer
{

  public static class ArbitrerExtensions
  {
    public static IServiceCollection AddArbitrer(this IServiceCollection services, Action<ArbitrerOptions> configure = null)
    {
      var options = new ArbitrerOptions();
      configure?.Invoke(options);

      services.AddSingleton(options);
      services.AddSingleton<IArbitrer, Arbitrer>();
      return services;
    }

    public static ArbitrerOptions SetAsLocalRequest<T>(this ArbitrerOptions options) where T : IBaseRequest
    {
      options.LocalRequests.Add(typeof(T));
      return options;
    }

    public static ArbitrerOptions SetAsRemoteRequest<T>(this ArbitrerOptions options) where T : IBaseRequest
    {
      options.RemoteRequests.Add(typeof(T));
      return options;
    }
  }
}