# Arbitrer
[![NuGet](https://img.shields.io/nuget/dt/arbitrer.svg)](https://www.nuget.org/packages/arbitrer) 
[![NuGet](https://img.shields.io/nuget/vpre/arbitrer.svg)](https://www.nuget.org/packages/arbitrer)


Arbitrer provides MediatR Pipelines to transform mediator from In-process to Out-Of-Process messaging via RPC calls implemented with popular message dispatchers. 

# When you need Arbitrer. 

Mediatr is very good to implement some patterns like [CQRS](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

Implementing CQRS in an in-process application does not bring to you all the power of the pattern but gives you the opportunity to have organized, easy to maintain code. When the application grow you may need to refactor your things to a microservices architecture.

Microservices and patterns like CQRS are very powerfull combination. In this scenario you will need to rewrite communication part to use some kind of out of process message dispatcher.

Arbitrer change Mediatr behaviour and let you decide which call needs to be in-process and which needs to be Out-of-process and dispatched remotely, via a configuration without changing a single row of your code.


# Installation



# RabbitMQ




# Kafka

Coming soon. 
