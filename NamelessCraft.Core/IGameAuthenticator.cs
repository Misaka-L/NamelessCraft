using NamelessCraft.Core.Models;

namespace NamelessCraft.Core;

public interface IGameAuthenticator
{
    public Task<GameAuthenticationResult> AuthenticateAsync();
}