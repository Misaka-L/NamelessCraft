using static System.IO.Path;

namespace NamelessCraft.Core.Models;

public record JvmRuntime(string FullVersion, long MajorVersion, string Path)
{
    public string GetJavaExecutable()
    {
        return Join(Path, "bin", OperatingSystem.IsWindows() ? "javaw.exe" : "java");
    }
}