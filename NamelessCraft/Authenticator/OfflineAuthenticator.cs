using NamelessCraft.Core;
using NamelessCraft.Core.Models;
using NamelessCraft.Tools;

namespace NamelessCraft.Authenticator;

public class OfflineAuthenticator : IGameAuthenticator
{
    public string UserName { get; private set; }
    public Guid Uuid { get; private set; }

    public OfflineAuthenticator(string userName)
    {
        UserName = userName;
        Uuid = UuidTools.GetOfflineUuid(userName);
    }

    public OfflineAuthenticator(string userName, Guid uuid)
    {
        UserName = userName;
        Uuid = uuid;
    }
    
    public Task<GameAuthenticationResult> AuthenticateAsync()
    {
        return Task.FromResult(new GameAuthenticationResult(UserName, "offline", Uuid, AuthenticationType.Mojang));
    }
}