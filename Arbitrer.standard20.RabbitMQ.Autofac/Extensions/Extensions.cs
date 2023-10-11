using System;
using System.Security.Cryptography;
using System.Text;
using Arbitrer.RabbitMQ;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Arbitrer
{
  public static class Extensions
  {
    public static ContainerBuilder AddArbitrerRabbitMQMessageDispatcher(this ContainerBuilder services, Action<MessageDispatcherOptions> config)
    {
      var options = new MessageDispatcherOptions();
      config(options);
      services.RegisterInstance(Options.Create(options)).SingleInstance();
      services.RegisterType<MessageDispatcher>().As<IExternalMessageDispatcher>().SingleInstance();
      return services;
    }

    public static ContainerBuilder ResolveArbitrerCalls(this ContainerBuilder services)
    {
      services.RegisterType<RequestsManager>().AsSelf().As<IHostedService>().SingleInstance();
      return services;
    }

    public static string GetHash(this string input, HashAlgorithm hashAlgorithm)
    {
      byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
      var sBuilder = new StringBuilder();
      for (int i = 0; i < data.Length; i++)
      {
        sBuilder.Append(data[i].ToString("x2"));
      }
      
      return sBuilder.ToString();
    }
    
    public static string GetHash(this byte[] input, HashAlgorithm hashAlgorithm)
    {
      byte[] data = hashAlgorithm.ComputeHash(input);
      var sBuilder = new StringBuilder();
      for (int i = 0; i < data.Length; i++)
      {
        sBuilder.Append(data[i].ToString("x2"));
      }
      
      return sBuilder.ToString();
    }
  }
}