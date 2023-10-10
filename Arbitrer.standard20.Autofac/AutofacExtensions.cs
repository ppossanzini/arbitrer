using System;
using Autofac;
using MediatR;
using Microsoft.Extensions.Options;

namespace Arbitrer
{
  public static class AutofacExtensions
  {
    public static ContainerBuilder AddArbitrer(this ContainerBuilder builder, Action<ArbitrerOptions> configure = null)
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

      return builder;
    }
  }
}