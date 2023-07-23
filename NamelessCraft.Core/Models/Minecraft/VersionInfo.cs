using System.Text.RegularExpressions;
using NamelessCraft.Core.Tools;

namespace NamelessCraft.Core.Models.Minecraft;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

public class MinecraftVersionInfo
{
    [JsonPropertyName("arguments")] public ModernLaunchArgument? ModernLaunchArguments { get; set; }

    [JsonPropertyName("minecraftArguments")]
    public string? LegacyLaunchArguments { get; set; }

    [JsonPropertyName("assetIndex")] public AssetIndex AssetIndex { get; set; }

    [JsonPropertyName("inheritsFrom")] public string? InheritsFrom { get; set; }

    [JsonPropertyName("assets")] public string Assets { get; set; }

    [JsonPropertyName("complianceLevel")] public long? ComplianceLevel { get; set; }

    [JsonPropertyName("downloads")] public GameDownloadInfo DownloadInfo { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("javaVersion")] public JavaVersionRequirement? RequiredJavaVersion { get; set; }

    [JsonPropertyName("libraries")] public Library[] Libraries { get; set; }

    [JsonPropertyName("logging")] public LoggingOptions? LoggingOptions { get; set; }

    [JsonPropertyName("mainClass")] public string MainClass { get; set; }

    [JsonPropertyName("releaseTime")] public string ReleaseTime { get; set; }

    [JsonPropertyName("time")] public string Time { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    public static MinecraftVersionInfo? ParseFromFile(string jsonPath) =>
        Parse(File.ReadAllText(jsonPath));

    public static MinecraftVersionInfo? Parse(string jsonContent)
    {
        return JsonSerializer.Deserialize<MinecraftVersionInfo>(jsonContent,
            MinecraftVersionInfoJsonConverter.Settings);
    }
}

#region LaunchArgument

public class ModernLaunchArgument
{
    [JsonPropertyName("game")] public GameLaunchArgument[] GameLaunchArguments { get; set; }

    [JsonPropertyName("jvm")] public GameLaunchArgument[] JvmLaunchArguments { get; set; }
}

public struct GameLaunchArgument
{
    public OptionalLaunchArgument? OptionalLaunchArgument;
    public string String;

    public static implicit operator GameLaunchArgument(OptionalLaunchArgument optionalLaunchArgument) =>
        new GameLaunchArgument { OptionalLaunchArgument = optionalLaunchArgument };

    public static implicit operator GameLaunchArgument(string String) => new GameLaunchArgument { String = String };
}

public class OptionalLaunchArgument
{
    [JsonPropertyName("rules")] public OptionalRule[] Rules { get; set; }

    [JsonPropertyName("value")] public OptionalLaunchArgumentValue Value { get; set; }
}

public class OptionalRule
{
    [JsonPropertyName("action")] public OptionalLaunchArgumentRuleAction Action { get; set; }

    [JsonPropertyName("features")] public Dictionary<string, bool>? OptionalLaunchArgumentFeature { get; set; }

    [JsonPropertyName("os")] public OptionalOsRule? OptionalOsRule { get; set; }

    public bool IsRuleAllow(Dictionary<string, bool> features, string systemName = "",
        string systemArchitecture = "", string systemVersion = "") =>
        IsRuleAllow(this, features, systemName, systemArchitecture, systemVersion);

    public static bool IsRuleAllow(OptionalRule rule, Dictionary<string, bool> features, string systemName = "",
        string systemArchitecture = "", string systemVersion = "")
    {
        if (string.IsNullOrWhiteSpace(systemName)) systemName = OSVersionTools.GetOSName();
        if (string.IsNullOrWhiteSpace(systemArchitecture)) systemArchitecture = OSVersionTools.GetOSArchitecture();
        if (string.IsNullOrWhiteSpace(systemVersion)) systemVersion = OSVersionTools.GetOSVersion();

        if (rule.OptionalOsRule is { } osRule)
        {
            if (osRule.Arch is { } arch && arch != systemArchitecture) return IsRuleAllow(rule.Action, false);
            if (osRule.Name is { } name && name != systemName) return IsRuleAllow(rule.Action, false);
            if (osRule.VersionRegex is { } versionRegex && !Regex.IsMatch(systemVersion, versionRegex))
                return IsRuleAllow(rule.Action, false);
        }

        if (rule.OptionalLaunchArgumentFeature is not { } featuresRule)
            return IsRuleAllow(rule.Action, true);

        foreach (var (key, value) in featuresRule)
        {
            if (features.TryGetValue(key, out var featureValue) && featureValue != value)
            {
                return IsRuleAllow(rule.Action, false);
            }

            return IsRuleAllow(rule.Action, false);
        }

        return IsRuleAllow(rule.Action, true);
    }

    private static bool IsRuleAllow(OptionalLaunchArgumentRuleAction action, bool value) =>
        action == OptionalLaunchArgumentRuleAction.Allow && value;
}

#endregion

public class OptionalOsRule
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("arch")]
    public string? Arch { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("version")]
    public string? VersionRegex { get; set; }
}

public class AssetIndex
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("sha1")] public string? Sha1 { get; set; }

    [JsonPropertyName("size")] public long? Size { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("totalSize")]
    public long? TotalSize { get; set; }

    [JsonPropertyName("url")] public Uri Url { get; set; }
}

public class GameDownloadInfo
{
    [JsonPropertyName("client")] public GameDownloadFileInfo Client { get; set; }

    [JsonPropertyName("client_mappings")] public GameDownloadFileInfo? ClientMappings { get; set; }

    [JsonPropertyName("server")] public GameDownloadFileInfo? Server { get; set; }

    [JsonPropertyName("server_mappings")] public GameDownloadFileInfo? ServerMappings { get; set; }
}

public class GameDownloadFileInfo
{
    [JsonPropertyName("sha1")] public string? Sha1 { get; set; }

    [JsonPropertyName("size")] public long? Size { get; set; }

    [JsonPropertyName("url")] public Uri Url { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("path")]
    public string Path { get; set; }
}

public class JavaVersionRequirement
{
    [JsonPropertyName("component")] public string Component { get; set; }

    [JsonPropertyName("majorVersion")] public long MajorVersion { get; set; }
}

public class Library
{
    [JsonPropertyName("downloads")] public LibraryDownloads? Downloads { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("rules")]
    public OptionalRule[]? Rules { get; set; }

    [JsonPropertyName("extract")] public NativeLibraryExtractSetting? NativeLibraryExtractSetting { get; set; }

    [JsonPropertyName("natives")] public NativeLibraryInfo? NativeLibraryInfo { get; set; }
}

public class LibraryDownloads
{
    [JsonPropertyName("artifact")] public GameDownloadFileInfo? Artifact { get; set; }

    [JsonPropertyName("classifiers")] public Classifiers? Classifiers { get; set; }
}

public class Classifiers
{
    [JsonPropertyName("natives-linux")] public NativeLibraryInfo NativeLibraryInfoLinux { get; set; }

    [JsonPropertyName("natives-osx")] public NativeLibraryInfo NativeLibraryInfoOsx { get; set; }

    [JsonPropertyName("natives-windows")] public NativeLibraryInfo NativeLibraryInfoWindows { get; set; }
}

public class NativeLibraryInfo
{
    [JsonPropertyName("path")] public string Path { get; set; }

    [JsonPropertyName("sha1")] public string Sha1 { get; set; }

    [JsonPropertyName("size")] public long Size { get; set; }

    [JsonPropertyName("url")] public Uri Url { get; set; }
}

public class NativeLibraryExtractSetting
{
    [JsonPropertyName("exclude")] public string[] Exclude { get; set; }
}

public class LoggingOptions
{
    [JsonPropertyName("client")] public ClientLoggingOption ClientLoggingOption { get; set; }
}

public class ClientLoggingOption
{
    [JsonPropertyName("argument")] public string Argument { get; set; }

    [JsonPropertyName("file")] public AssetIndex File { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }
}

public struct OptionalLaunchArgumentValue
{
    public string? String;
    public string[]? StringArray;

    public static implicit operator OptionalLaunchArgumentValue(string String) =>
        new OptionalLaunchArgumentValue { String = String };

    public static implicit operator OptionalLaunchArgumentValue(string[] StringArray) =>
        new OptionalLaunchArgumentValue { StringArray = StringArray };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OptionalLaunchArgumentRuleAction
{
    Disallow,
    Allow
}

#region Converter

public static class MinecraftVersionInfoJsonConverter
{
    public static readonly JsonSerializerOptions? Settings = new(JsonSerializerDefaults.General)
    {
        Converters =
        {
            GameElementConverter.Singleton,
            ValueConverter.Singleton,
            new DateOnlyConverter(),
            new TimeOnlyConverter(),
            IsoDateTimeOffsetConverter.Singleton
        },
    };
}

internal class GameElementConverter : JsonConverter<GameLaunchArgument>
{
    public override bool CanConvert(Type t) => t == typeof(GameLaunchArgument);

    public override GameLaunchArgument Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                return new GameLaunchArgument { String = stringValue };
            case JsonTokenType.StartObject:
                var objectValue = JsonSerializer.Deserialize<OptionalLaunchArgument>(ref reader, options);
                return new GameLaunchArgument { OptionalLaunchArgument = objectValue };
        }

        throw new Exception("Cannot unmarshal type GameElement");
    }

    public override void Write(Utf8JsonWriter writer, GameLaunchArgument value, JsonSerializerOptions options)
    {
        if (value.String != null)
        {
            JsonSerializer.Serialize(writer, value.String, options);
            return;
        }

        if (value.OptionalLaunchArgument != null)
        {
            JsonSerializer.Serialize(writer, value.OptionalLaunchArgument, options);
            return;
        }

        throw new Exception("Cannot marshal type GameElement");
    }

    public static readonly GameElementConverter Singleton = new GameElementConverter();
}

internal class ValueConverter : JsonConverter<OptionalLaunchArgumentValue>
{
    public override bool CanConvert(Type t) => t == typeof(OptionalLaunchArgumentValue);

    public override OptionalLaunchArgumentValue Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                return new OptionalLaunchArgumentValue { String = stringValue };
            case JsonTokenType.StartArray:
                var arrayValue = JsonSerializer.Deserialize<string[]>(ref reader, options);
                return new OptionalLaunchArgumentValue { StringArray = arrayValue };
        }

        throw new Exception("Cannot unmarshal type Value");
    }

    public override void Write(Utf8JsonWriter writer, OptionalLaunchArgumentValue optionalLaunchArgumentValue,
        JsonSerializerOptions options)
    {
        if (optionalLaunchArgumentValue.String != null)
        {
            JsonSerializer.Serialize(writer, optionalLaunchArgumentValue.String, options);
            return;
        }

        if (optionalLaunchArgumentValue.StringArray != null)
        {
            JsonSerializer.Serialize(writer, optionalLaunchArgumentValue.StringArray, options);
            return;
        }

        throw new Exception("Cannot marshal type Value");
    }

    public static readonly ValueConverter Singleton = new ValueConverter();
}

public class DateOnlyConverter : JsonConverter<DateOnly>
{
    private readonly string serializationFormat;

    public DateOnlyConverter() : this(null)
    {
    }

    public DateOnlyConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}

public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private readonly string serializationFormat;

    public TimeOnlyConverter() : this(null)
    {
    }

    public TimeOnlyConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
    }

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return TimeOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}

internal class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

    private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

    private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
    private string? _dateTimeFormat;
    private CultureInfo? _culture;

    public DateTimeStyles DateTimeStyles
    {
        get => _dateTimeStyles;
        set => _dateTimeStyles = value;
    }

    public string? DateTimeFormat
    {
        get => _dateTimeFormat ?? string.Empty;
        set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
    }

    public CultureInfo Culture
    {
        get => _culture ?? CultureInfo.CurrentCulture;
        set => _culture = value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        string text;


        if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
            || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
        {
            value = value.ToUniversalTime();
        }

        text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

        writer.WriteStringValue(text);
    }

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateText = reader.GetString();

        if (string.IsNullOrEmpty(dateText) == false)
        {
            if (!string.IsNullOrEmpty(_dateTimeFormat))
            {
                return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
            }
            else
            {
                return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
            }
        }
        else
        {
            return default(DateTimeOffset);
        }
    }


    public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
}

#endregion