using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ceen.Httpd.Handler;

namespace Vrc.OscQuery;

public class EmbeddedResourcesVfs : FileHandler.IVirtualFileSystem
{
    private readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
    private readonly DateTime myInitTime = DateTime.Now;
    private readonly string myResourcePrefix;

    public EmbeddedResourcesVfs(string resourcePrefix)
    {
        myResourcePrefix = resourcePrefix;
    }

    public Task<bool> FileExistsAsync(string path)
    {
        path = path.TrimStart('/');
        var info = myAssembly.GetManifestResourceInfo($"{myResourcePrefix}.{path}");
        return Task.FromResult(info != null);
    }

    public Task<bool> FolderExistsAsync(string path)
    {
        return Task.FromResult(path == "/");
    }

    public Task<DateTime> GetLastFileWriteTimeUtcAsync(string path)
    {
        return Task.FromResult(myInitTime);
    }

    public Task<Stream> OpenReadAsync(string path)
    {
        path = path.TrimStart('/');
        return Task.FromResult(myAssembly.GetManifestResourceStream($"{myResourcePrefix}.{path}")!);
    }

    public Task<string> GetMimeTypePathAsync(string path)
    {
        return Task.FromResult(path);
    }
}