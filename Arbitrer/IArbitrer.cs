using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;

namespace Arbitrer
{
  public interface IArbitrer
  {
    bool HasLocalHandler<T>() where T : IBaseRequest;
    bool HasLocalHandler(Type t);
    bool HasRemoteHandler<T>() where T : IBaseRequest;
    bool HasRemoteHandler(Type t);
    HandlerLocation GetLocation(Type t);
    HandlerLocation GetLocation<T>();
    Task<TResponse> InvokeRemoteHandler<TRequest, TResponse>(TRequest request, string queueName = null); 
    Task SendRemoteNotification<TRequest>(TRequest request, string queueName = null) where TRequest : INotification;

    IEnumerable<Type> GetLocalRequestsTypes();
    IEnumerable<Type> GetRemoteRequestsTypes();
  }
}