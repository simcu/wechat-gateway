using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Wechat.Grpcs;
using Wechat.Helpers;
using System;
using Grpc.Core.Logging;

namespace Grpcs.Gateway.Wechat
{
    public static class GrpcMiddlewareExtensions
    {
        public static IApplicationBuilder UseGrpc(this IApplicationBuilder builder, IDatabase redis, IConfiguration config,
                                                  MessageQueue messageQueue, EventQueue eventQueue)
        {
            GrpcEnvironment.SetLogger(new ConsoleLogger());
            var grpcServer = new Server
            {
                Services = {
                    Component.BindService(new ComponentImpl(redis,config)),
                    Message.BindService(new MessageImpl(messageQueue,eventQueue,redis)),
                    WxWeb.BindService(new WxWebImpl(redis,config)),
                    WxApp.BindService(new WxAppImpl(redis,config))
                },
                Ports = { new ServerPort(config["Grpc:Host"], int.Parse(config["Grpc:Port"]), ServerCredentials.Insecure) }
            };
            grpcServer.Start();
            Console.WriteLine("Grpc listening on: tcp://{0}:{1}", config["Grpc:Host"], config["Grpc:Port"]);
            return builder;
        }
    }
}
