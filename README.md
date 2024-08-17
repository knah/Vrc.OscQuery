# Vrc.OscQuery
This is a fork of VRChat's [vrc-oscquery-lib](https://github.com/vrchat-community/vrc-oscquery-lib) (MIT license), with the following changes:
* Removed everything Unity-related, because...
* Upgraded to net8 and netstandard as the only target frameworks.
* The netstandard2 build may work in Unity as a dll, but will likely fail to compile due to using newer C# language version.
* Replaced `Newtonsoft.Json` with `System.Text.Json` for better AOT compatibility.
* Replaced `HttpListener` with `Ceen.Httpd`, which allows to bind to non-localhost adapters.
* `Ceen.Httpd` is also cross-platform, which is not a given for `HttpListener`
* Added support for OSCQuery `path?ATTRIBUTE` queries for most attributes.
* Added support for OSCQuery `RANGE` attribute.
* Upgraded `MeaModDiscovery` to list all IPs for a given service and expire services based on advertised TTL.
* Tossed `OscQueryServiceBuilder` because it wasn't a proper builder anyway.
* A lot of renaming to fit a different code style. This makes this library not-a-drop-in replacement!

# Sample usage
```csharp
var discovery = new MeaModDiscovery();
discovery.OnAnyOscServiceRemoved += p =>
    Logger.LogInformation("Service '{Name}' of type {Type} at {Address}:{Port} left", p.Name, p.Type, p.Address, p.Port);
discovery.OnAnyOscServiceAdded += p => 
    Logger.LogInformation("Found service '{Name}' of type {Type} at {Address}:{Port}", p.Name, p.Type, p.Address, p.Port);

var oscSocket = new OscSocket(new IPEndPoint(bindAddress, 0)); // depends on your OSC lib
Logger.LogInformation("OSC socket listening on {Address}:{Port}", bindAddress, oscSocket.Port);

var service = new OscQueryService($"AmazingService-{Random.Shared.Next():X8}", bindAddress, discovery)
    .AdvertiseOscService(oscSocket.Port)
    .WithEndpoint("/avatar/change", "s", Attributes.AccessValues.Write)
    .StartHttpServer();

service.RefreshServices();
```