using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ceen;
using Ceen.Httpd;
using Ceen.Httpd.Handler;
using Microsoft.Extensions.Logging;

namespace Vrc.OscQuery
{
    public sealed class OscQueryHttpServer : IDisposable
    {
        private readonly ILogger<OscQueryService> myLogger;
        private readonly OscQueryService myOscQuery;
        private readonly CancellationTokenSource myTokenSource = new();
        
        public OscQueryHttpServer(OscQueryService oscQueryService, ILogger<OscQueryService> logger)
        {
            myOscQuery = oscQueryService;
            myLogger = logger;

            var serverConfig = new ServerConfig();
            serverConfig.AddLogger((context, exception, started, duration) =>
            {
                if (exception != null)
                    logger.LogError(exception, "Error while processing request from {Source} to {Path}", context.Request.RemoteEndPoint, context.Request.Path);
                
                return Task.CompletedTask;
            });
            serverConfig.AddRoute($"/{Attributes.HOST_INFO}", HandleHostInfoNew);
            serverConfig.AddRoute("/*", new FileHandler(new EmbeddedResourcesVfs("Vrc.OscQuery.Resources")) { PassThrough = true, SourceFolder = "/" });
            serverConfig.AddRoute("/*", HandleRootNew);

            HttpServer.ListenAsync(new IPEndPoint(myOscQuery.HostIp, myOscQuery.TcpPort), false, serverConfig, myTokenSource.Token);
            
            myLogger.LogInformation("OSCQuery server listening on {Endpoint}:{Port}", myOscQuery.HostIp, myOscQuery.TcpPort);
        }
        
        private async Task<bool> HandleHostInfoNew(IHttpContext context)
        {
            try
            {
                var hostInfoString = myOscQuery.HostInfo.ToJson();
                
                context.Response.Headers.Add("pragma", "no-cache");
                await context.Response.WriteAllJsonAsync(hostInfoString);
                return true;
            }
            catch (Exception e)
            {
                myLogger.LogError($"Could not construct and send Host Info: {e.Message}");
            }

            return false;
        }
        
        private async Task<bool> HandleRootNew(IHttpContext context)
        {
            var path = context.Request.Path;
            var requestedAttribute = context.Request.RawQueryString?.TrimStart('?');
            if (requestedAttribute == Attributes.HOST_INFO)
                return await HandleHostInfoNew(context);

            var matchedNode = myOscQuery.RootNode.GetNodeWithPath(path);
            if (matchedNode == null)
            {
                return context.SetResponseNotFound("OSC Path not found");
            }
            
            context.Response.Headers.Add("pragma", "no-cache");

            switch (requestedAttribute)
            {
                case null:
                case "":
                    await context.Response.WriteAllJsonAsync(matchedNode.ToJson());
                    return true;
                case Attributes.VALUE:
                    await context.Response.WriteAllJsonAsync(JsonSerializer.Serialize(matchedNode.Value));
                    return true;
                case Attributes.TYPE:
                    await context.Response.WriteAllJsonAsync(JsonSerializer.Serialize(matchedNode.OscType));
                    return true;
                case Attributes.ACCESS:
                    await context.Response.WriteAllJsonAsync(JsonSerializer.Serialize((int) matchedNode.Access));
                    return true;
                case Attributes.DESCRIPTION:
                    await context.Response.WriteAllJsonAsync(JsonSerializer.Serialize(matchedNode.Description));
                    return true;
                case Attributes.FULL_PATH:
                    await context.Response.WriteAllJsonAsync(JsonSerializer.Serialize(matchedNode.FullPath));
                    return true;
                default:
                    return context.SetResponseBadRequest($"Unknown attribute {requestedAttribute}");
            }
        }

        public void Dispose()
        {
            myTokenSource.Dispose();
        }
    }
}