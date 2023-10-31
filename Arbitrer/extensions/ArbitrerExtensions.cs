using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arbitrer
{
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
  public static class ArbitrerExtensions
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

    public static IServiceCollection AddArbitrer(this ServiceCollection services, IEnumerable<Assembly> assemblies)
    {
      services.AddArbitrer(cfg =>
      {
        cfg.Behaviour = ArbitrerBehaviourEnum.ImplicitRemote;
        cfg.InferLocalRequests(assemblies);
        cfg.InferLocalNotifications(assemblies);
        cfg.InferPublishedNotifications(assemblies);
      });
      return services;
    }

    public static ArbitrerOptions InferLocalRequests(this ArbitrerOptions options, IEnumerable<Assembly> assemblies)
    {
      var localRequests = assemblies.SelectMany(a => a
        .GetTypes()
        .SelectMany(t => t.GetInterfaces()
          .Where(i => i != null && i.FullName != null && i.FullName.StartsWith("MediatR.IRequestHandler"))
          .Select(i => i.GetGenericArguments()[0]).ToArray()
        ));
      options.SetAsLocalRequests(localRequests.ToArray);
      return options;
    }

    public static ArbitrerOptions InferPublishedNotifications(this ArbitrerOptions options, IEnumerable<Assembly> assemblies)
    {
      var localNotifications = assemblies.SelectMany(a => a
        .GetTypes()
        .SelectMany(t => t.GetInterfaces()
          .Where(i => i != null && i.FullName != null && i.FullName.StartsWith("MediatR.INotification`"))
          .Select(i => i.GetGenericArguments()[0]).ToArray()
        ));

      options.SetAsRemoteRequests(() => localNotifications);
      return options;
    }

    public static ArbitrerOptions InferLocalNotifications(this ArbitrerOptions options, IEnumerable<Assembly> assemblies)
    {
      var localNotifications = assemblies.SelectMany(a => a
        .GetTypes()
        .SelectMany(t => t.GetInterfaces()
          .Where(i => i != null && i.FullName != null && i.FullName.StartsWith("MediatR.INotificationHandler"))
          .Select(i => i.GetGenericArguments()[0]).ToArray()
        ));

      options.SetAsLocalRequests(() => localNotifications);
      return options;
    }

    public static ArbitrerOptions SetAsLocalRequest<T>(this ArbitrerOptions options) where T : IBaseRequest
    {
      options.LocalRequests.Add(typeof(T));
      return options;
    }

    public static ArbitrerOptions ListenForNotification<T>(this ArbitrerOptions options) where T : INotification
    {
      options.LocalRequests.Add(typeof(T));
      return options;
    }

    public static ArbitrerOptions SetAsRemoteRequest<T>(this ArbitrerOptions options) where T : IBaseRequest
    {
      options.RemoteRequests.Add(typeof(T));
      return options;
    }

    public static ArbitrerOptions PropagateNotification<T>(this ArbitrerOptions options) where T : INotification
    {
      options.RemoteRequests.Add(typeof(T));
      return options;
    }

    public static ArbitrerOptions SetAsLocalRequests(this ArbitrerOptions options, Func<IEnumerable<Assembly>> assemblySelect)
    {
      var types = (from a in assemblySelect()
        from t in a.GetTypes()
        where typeof(IBaseRequest).IsAssignableFrom(t) || typeof(INotification).IsAssignableFrom(t)
        select t).AsEnumerable();
      foreach (var t in types)
        options.LocalRequests.Add(t);
      return options;
    }

    public static ArbitrerOptions SetAsLocalRequests(this ArbitrerOptions options, Func<IEnumerable<Type>> typesSelect)
    {
      foreach (var t in typesSelect())
        options.LocalRequests.Add(t);
      return options;
    }

    public static ArbitrerOptions SetAsRemoteRequests(this ArbitrerOptions options, Func<IEnumerable<Assembly>> assemblySelect)
    {
      var types = (from a in assemblySelect()
        from t in a.GetTypes()
        where typeof(IBaseRequest).IsAssignableFrom(t) || typeof(INotification).IsAssignableFrom(t)
        select t).AsEnumerable();
      foreach (var t in types)
        options.RemoteRequests.Add(t);
      return options;
    }

    public static ArbitrerOptions SetAsRemoteRequests(this ArbitrerOptions options, Func<IEnumerable<Type>> typesSelect)
    {
      foreach (var t in typesSelect())
        options.RemoteRequests.Add(t);
      return options;
    }

    public static string TypeQueueName(this Type t, StringBuilder sb = null)
    {
      if (t.CustomAttributes.Any())
      {
        var attr = t.GetCustomAttribute<ArbitrerQueueNameAttribute>();
        if (attr != null) return $"{t.Namespace}.{attr.Name}".Replace(".", "_");
      }

      sb = sb ?? new StringBuilder();
      sb.Append(t.Namespace);
      sb.Append(".");
      sb.Append(t.Name);

      if (t.GenericTypeArguments?.Length > 0)
      {
        sb.Append("[");
        foreach (var ta in t.GenericTypeArguments)
        {
          ta.TypeQueueName(sb);
          sb.Append(",");
        }

        sb.Append("]");
      }

      return sb.ToString().Replace(",]", "]").Replace(".", "_");
    }
  }
}