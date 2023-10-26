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
    HandlerLocation GetLocation<T>() where T : IBaseRequest;
    Task<TResponse> InvokeRemoteHandler<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse>;
    Task SendRemoteNotification<TRequest>(TRequest request) where TRequest : INotification;

    IEnumerable<Type> GetLocalRequestsTypes();
    IEnumerable<Type> GetRemoteRequestsTypes();
  }
}