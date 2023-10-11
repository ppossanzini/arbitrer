using System;
using Autofac;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbitrer
{
  public static class AutofacExtensions
  {
    public static ContainerBuilder AddArbitrer(this ContainerBuilder builder, Action<ArbitrerOptions> configure, ILoggerFactory loggerFactory)
    {
      if (configure != null)
      {
        var options = new ArbitrerOptions();
        configure(options);
        var opt = Options.Create<ArbitrerOptions>(options);

        builder.RegisterInstance(opt).SingleInstance();
      }

      builder.RegisterType<Arbitrer>().As<IArbitrer>().SingleInstance();
      builder.RegisterType<ArbitredMediatr>().As<IMediator>();
      builder.RegisterGeneric(typeof(Pipelines.ArbitrerPipeline<,>)).As(typeof(IPipelineBehavior<,>));

      builder.RegisterInstance(LoggerFactoryExtensions.CreateLogger<ArbitredMediatr>(loggerFactory)).As<ILogger<ArbitredMediatr>>();
      builder.RegisterInstance(LoggerFactoryExtensions.CreateLogger<Arbitrer>(loggerFactory)).As<ILogger<Arbitrer>>();

      return builder;
    }
  }
}