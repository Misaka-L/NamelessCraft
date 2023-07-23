using System.Security.Cryptography;
using System.Text;

namespace NamelessCraft.Tools;

public static class UuidTools
{
    public static Guid GetOfflineUuid(string userName)
    {
        // https://github.com/BakaXL-Launcher/BakaXL.Core/blob/master/BakaXL.Core/Tools/UUIDConverter.cs
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"OfflinePlayer:{userName}"));
        hash[6] &= 0x0f;
        hash[6] |= 0x30;
        hash[8] &= 0x3f;
        hash[8] |= 0x80;
        (hash[7], hash[6]) = (hash[6], hash[7]);
        (hash[5], hash[4]) = (hash[4], hash[5]);
        (hash[3], hash[0]) = (hash[0], hash[3]);
        (hash[2], hash[1]) = (hash[1], hash[2]);
        
        return new Guid(hash);
    }

    public static string ToMinecraftUuid(this Guid uuid)
    {
        return uuid.ToString("N");
    }
}