using System.Text.Json;
using NamelessCraft;
using NamelessCraft.Authenticator;
using NamelessCraft.Core.Models.Minecraft;
using NamelessCraft.Models.Options;
using NamelessCraft.Tools;

var launcher = new NamelessLauncher(options =>
{
    options.Authenticator = new OfflineAuthenticator("nameless");

    options.MinecraftVersionInfo =
        MinecraftVersionInfo.ParseFromFile(@"D:\Minecraft\BakaXL\.minecraft\versions\VTMCraft-2023\VTMCraft-2023.json",
            @"D:\Minecraft\BakaXL\.minecraft\versions\");
    options.GameDirectory = "D:/Minecraft/BakaXL/.minecraft/versions/VTMCraft-2023/";
    options.AssetsDirectoryPath = "D:/Minecraft/BakaXL/.minecraft/assets";
    options.LibrariesDirectoryPath = "D:/Minecraft/BakaXL/.minecraft/libraries";
    options.VersionJarPath = "D:/Minecraft/BakaXL/.minecraft/versions/1.20.1/1.20.1.jar";
});

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

launcher.GameOutputDataReceived += (_, eventArgs) => Console.WriteLine(eventArgs.Data);
launcher.GameExited += (_, _) => cancellationTokenSource.Cancel();

await launcher.StartAsync();

try
{
    await Task.Delay(Timeout.Infinite, cancellationToken);
}
catch (TaskCanceledException e)
{
}

// Console.WriteLine(JsonSerializer.Serialize(await JvmTools.LookupJvmRuntimesAsync(), new JsonSerializerOptions()
// {
//     WriteIndented = true
// }));

//await launcher.Options.Authenticator.AuthenticateAsync();