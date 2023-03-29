# Arbitrer
[![NuGet](https://img.shields.io/nuget/dt/arbitrer.svg)](https://www.nuget.org/packages/arbitrer) 
[![NuGet](https://img.shields.io/nuget/vpre/arbitrer.svg)](https://www.nuget.org/packages/arbitrer)


Arbitrer provides [MediatR](https://github.com/jbogard/MediatR) Pipelines to transform mediator from In-process to Out-Of-Process messaging via RPC calls implemented with popular message dispatchers. 

## When you need Arbitrer. 

Mediatr is very good to implement some patterns like [CQRS](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

Implementing CQRS in an in-process application does not bring to you all the power of the pattern but gives you the opportunity to have organized, easy to maintain code. When the application grow you may need to refactor your things to a microservices architecture.

Microservices and patterns like CQRS are very powerfull combination. In this scenario you will need to rewrite communication part to use some kind of out of process message dispatcher.

Arbitrer change Mediatr behaviour and let you decide which call needs to be in-process and which needs to be Out-of-process and dispatched remotely, via a configuration without changing a single row of your code.


## Installation

You should install [Arbitrer with NuGet](https://www.nuget.org/packages/arbitrer):

    Install-Package Arbitrer
    
Or via the .NET Core command line interface:

    dotnet add package Arbitrer

Either commands, from Package Manager Console or .NET Core CLI, will download and install Arbitrer and all required dependencies.


## Basic Configuration 

Configuring Arbitrer is an easy task. 
1) Add Arbitrer to services configuration via AddArbitrer extension method. 

``` 
  services.AddArbitrer(opt => ...
```

2) Decide what is the default behaviour, available options are 
   1) ***ImplicitLocal*** : all `mediator.Send()` calls will be delivered in-process unless further configuration. 
   2) ***ImplicitRemote*** : all `mediator.Send()` calls will be delivered out-of-process unless further configuration. 
   3) ***Explicit*** : you have the responsability do declare how to manage every single call. 


```
    services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.Explicit;
    });
```


3) Configure calls delivery type according with you behaviour:

```
    services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.Explicit;
      opt.SetAsRemoteRequest<MediatRRequest1>();
      opt.SetAsRemoteRequest<MediatRRequest2>();
      ....
    }
```


Of course you will have some processes with requests declared **Local** and other processes with same requests declared **Remote**. 


### Example of process with all local calls and some remote calls

```
    services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.ImplicitLocal;
      opt.SetAsRemoteRequest<MediatRRequest1>();
      opt.SetAsRemoteRequest<MediatRRequest2>();
      opt.SetAsRemoteRequests(typeof(MediatRRequest2).Assembly); // All requests in an assembly
    });
```


### Example of process with local handlers. 

```
    services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.ImplicitLocal;
    });

```

### Example of process with remore handlers. 

```
    services.AddArbitrer(opt =>
    {
      opt.Behaviour = ArbitrerBehaviourEnum.ImplicitRemote;
    });
```


# Arbitrer with RabbitMQ


## Installing Arbitrer RabbitMQ extension.

```
    Install-Package Arbitrer.RabbitMQ
```
    
Or via the .NET Core command line interface:

```
    dotnet add package Arbitrer.RabbitMQ
```

## Configuring RabbitMQ Extension. 

Once installed you need to configure rabbitMQ extension. 

```
    services.AddArbitrerRabbitMQMessageDispatcher(opt =>
    {
      opt.HostName = "rabbit instance";
      opt.Port = 5672;
      opt.Password = "password";
      opt.UserName = "rabbituser";
      opt.VirtualHost = "/";
    });
    services.ResolveArbitrerCalls();
```

or if you prefer use appsettings configuration 

```
    services.AddArbitrerRabbitMQMessageDispatcher(opt => context.Configuration.GetSection("rabbitmq").Bind(opt));
    services.ResolveArbitrerCalls();
```


# Arbitrer with Kafka

## Installing Arbitrer Kafka extension.

```
    Install-Package Arbitrer.Kafka
```
    
Or via the .NET Core command line interface:

```
    dotnet add package Arbitrer.Kafka
```


## Configuring Kafka Extension. 

Once installed you need to configure Kafka extension. 

```
    services.AddArbitrerKafkaMessageDispatcher(opt =>
    {
      opt.BootstrapServers = "localhost:9092";
    });
    services.ResolveArbitrerCalls();
```

or if you prefer use appsettings configuration 

```
    services.AddArbitrerKafkaMessageDispatcher(opt => context.Configuration.GetSection("kafka").Bind(opt));
    services.ResolveArbitrerCalls();
```



# Arbitrer with Azure Message Queues

Coming soon. 
