using sender;
using Arbitrer;
using cqrs.models.Commands;
using System.Reflection;
using MediatR;


IHost host = Host.CreateDefaultBuilder(args)
  .ConfigureServices((context, services) =>
  {
    services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()); });
    services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.Explicit;
      opt.SetAsRemoteRequest<MediatRRequest1>();
      opt.SetAsRemoteRequest<MediatRRequest2>();
      opt.SetAsRemoteRequest<MediatRRequest3>();
      opt.SetAsRemoteRequest<MediatRRequest4>();
      opt.SetAsRemoteRequest<MediatRRequest5>();
      opt.SetAsRemoteRequest<MediatRRequestWithException>();
      opt.SetAsRemoteRequest<MediatRRequestWithHandlerException>();
      opt.SetAsRemoteRequest<MediatRRequestWithNoHandlers>();
    });
    // services.AddArbitrerRabbitMQMessageDispatcher(opt => context.Configuration.GetSection("rabbitmq").Bind(opt));
    services.AddArbitrerKafkaMessageDispatcher(opt => context.Configuration.GetSection("kafka").Bind(opt));

    services.AddHostedService<Worker>();
  })
  .Build();


await host.RunAsync();