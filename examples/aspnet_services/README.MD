## Project Structure

This example show how to use arbitrer in a ASPNET microservices application that use MediatR ,CQRS pattern and microservices. 

https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api


Every  xxx.CQRS projects contains only Requests and Notification definition 
In a CQRS pattern you need DTO to transport your data and classes to describe the command. 
CQRS pattern implementation divide Commands and Queries, 
so in xxx.CQRS projects you will find:

* COMMAND folder with Request classes to describe commands that ONLY change some data
(but does not never return valuable result, only simple informations)
* QUERY folder with Request classes to describe operation needed to search and retrieve data
* NOTIFICATION folder with all Notification classes to propagate event in the application 
* DTO to create Data Transport Objects you need to move your data inside the application. 

xxx.CQRS project does not contains any kink of method or mapping, but only command/query definitions 
so you can reference CQRS project without compromising code separation 

In the example you can found services implemented using different tecniques and different levels of code separation. 
Differet examples use Arbitrer in a different way. 

### AllinOne

You can find 4 projects: 

* Service1: Contains only configuration and dependency injection to startup the application, Arbitrer configuration is here. 
* Service1.API: Contains only Controllers to expose needed method to via rest services and mapping to expose correct DTO
* Service1.CQRS: Contains CQRS definitions
* Service1.Handlers: Contains Handlers and all business application. DB access and external resources access code is here. 

This separation is useful to correctly divide business logic and communication logic, it's also useful to avoid 
reference mistakes that can create problems in your application.   

#### Service1.API
A little detail about API project, it contains only REST enabled controllers and some DTO you can use to expose your data to the world
It's not always a good idea expose CQRS Dto because DTO in CQRS project can contains security and sensible data. 
Dto in xxx.CQRS projects are used to trasport data inside the application and not outside. 

Thi project can contain also mapping configuration from local DTO to CQRS DTOs.


#### Service1.Handlers

