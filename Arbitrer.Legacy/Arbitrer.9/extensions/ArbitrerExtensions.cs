using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arbitrer
{
  public static class ArbitrerExtensions
  {
    public static ArbitrerOptions InferLocalRequests(this ArbitrerOptions options, IEnumerable<Assembly> assemblies)
    {
      var localRequests = assemblies.SelectMany(a => a
        .GetTypes()
        .SelectMany(t => t.GetInterfaces()
          .Where(i => i.FullName != null && i.FullName.StartsWith("MediatR.IRequestHandler"))
          .Select(i => i.GetGenericArguments()[0]).ToArray()
        ));
      options.SetAsLocalRequests(() => localRequests);
      return options;
    }

    // [Obsolete("This registration is no longer needed", false)]
    // public static ArbitrerOptions InferPublishedNotifications(this ArbitrerOptions options, IEnumerable<Assembly> assemblies)
    // {
    //   var localNotifications = assemblies.SelectMany(a => a
    //     .GetTypes()
    //     .SelectMany(t => t.GetInterfaces()
    //       .Where(i => i.FullName != null && i.FullName.StartsWith("MediatR.INotification") && !i.FullName.StartsWith("MediatR.INotificationHandler"))
    //       .ToArray()
    //     ));
    //
    //   options.SetAsRemoteRequests(() => localNotifications);
    //   return options;
    // }

    public static ArbitrerOptions InferLocalNotifications(this ArbitrerOptions options, IEnumerable<Assembly> assemblies)
    {
      var localNotifications = assemblies.SelectMany(a => a
        .GetTypes()
        .SelectMany(t => t.GetInterfaces()
          .Where(i => i.FullName != null && i.FullName.StartsWith("MediatR.INotificationHandler"))
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

    // [Obsolete("This registration is no longer needed", false)]
    // public static ArbitrerOptions PropagateNotification<T>(this ArbitrerOptions options) where T : INotification
    // {
    //   options.RemoteRequests.Add(typeof(T));
    //   return options;
    // }

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

    public static string TypeQueueName(this Type t, ArbitrerOptions options, StringBuilder sb = null)
    {
      if (t.CustomAttributes.Any())
      {
        var attr = t.GetCustomAttribute<ArbitrerQueueNameAttribute>();
        if (attr != null) return $"{t.Namespace}.{attr.Name}".Replace(".", "_");
      }

      options.QueuePrefixes.TryGetValue(t.FullName, out var prefix);
      prefix = prefix ?? options.DefaultQueuePrefix;

      sb = sb ?? new StringBuilder();

      if (!string.IsNullOrWhiteSpace(prefix)) sb.Append($"{prefix}.");
      sb.Append($"{t.Namespace}.{t.Name}");

      if (t.GenericTypeArguments?.Length > 0)
      {
        sb.Append("[");
        foreach (var ta in t.GenericTypeArguments)
        {
          ta.TypeQueueName(options, sb);
          sb.Append(",");
        }

        sb.Append("]");
      }

      return sb.ToString().Replace(",]", "]").Replace(".", "_");
    }
    
    public static int? QueueTimeout(this Type t)
    {
      if (t.CustomAttributes.Any())
      {
        var attr = t.GetCustomAttribute<ArbitrerQueueTimeoutAttribute>();
        if (attr != null) return attr.ConsumerTimeout;
      }

      return null;
    }
  }
}