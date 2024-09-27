using System;
using System.Security.Cryptography;
using System.Text;
using Arbitrer.GRPC;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arbitrer.GRPC.Extensions
{
  
  
  public static class Extensions
  {
    
    public const string ArbitrerGrpcCorsDefaultPolicy = "ArbitrerGRPCDefault";

    public static void AddArbitrerGrpcCors(this CorsOptions corsOptions)
    {
      corsOptions.AddPolicy(ArbitrerGrpcCorsDefaultPolicy, builder =>
      {
        builder.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
      });
    }


    /// <summary>
    /// Add the Arbitrer RabbitMQ message dispatcher to the service collection, allowing it to be resolved and used.
    /// </summary>
    /// <param name="services">The service collection to add the message dispatcher to.</param>
    /// <param name="config">The configuration settings for the message dispatcher.</param>
    /// <param name="grpcOptions">Grpc service configuration options, if not specified you need to call AddGrpc()
    /// registration method.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddArbitrerGrpcDispatcher(this IServiceCollection services, 
      Action<MessageDispatcherOptions> config, 
      Action<GrpcServiceOptions> grpcOptions = null)
    {

      if (grpcOptions != null)
      {
        services.Configure(grpcOptions);
        services.AddGrpc();
      }

      services.AddCors(o => o.AddArbitrerGrpcCors());
      
      services.Configure<MessageDispatcherOptions>(config);
      services.AddSingleton<IExternalMessageDispatcher, MessageDispatcher>();

      return services;
    }
    
    public static IEndpointRouteBuilder UseGrpcRequestManager(this IEndpointRouteBuilder host)
    {
      host.MapGrpcService<RequestsManager>();
      return host;
    }

    public static IEndpointRouteBuilder UseGrpcWebRequestsManager(this IEndpointRouteBuilder host)
    {
      host.MapGrpcService<RequestsManager>()
        .EnableGrpcWeb()
        .RequireCors(ArbitrerGrpcCorsDefaultPolicy);
      return host;
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
  }
}