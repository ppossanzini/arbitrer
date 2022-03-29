using sender;
using Arbitrer;
using cqrs.models.Commands;
using System.Reflection;
using MediatR;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {

      services.AddMediatR(Assembly.GetExecutingAssembly());
      services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.Explicit;
      opt.SetAsRemoteRequest<MediatRRequest1>();
    });
      services.AddArbitrerRabbitMQMessageDispatcher(opt => context.Configuration.GetSection("rabbitmq").Bind(opt));

      services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
