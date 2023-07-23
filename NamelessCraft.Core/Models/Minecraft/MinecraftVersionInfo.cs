using System.Text.RegularExpressions;
using NamelessCraft.Core.Tools;

namespace NamelessCraft.Core.Models.Minecraft;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

public class MinecraftVersionInfo
{
    [JsonPropertyName("arguments")] public MinecraftModernLaunchArgument? ModernLaunchArguments { get; set; }

    [JsonPropertyName("minecraftArguments")]
    public string? LegacyLaunchArguments { get; set; }

    [JsonPropertyName("assetIndex")] public AssetIndex? AssetIndex { get; set; }

    [JsonPropertyName("inheritsFrom")] public string? InheritsFrom { get; set; }

    [JsonPropertyName("assets")] public string? Assets { get; set; }

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

    public static MinecraftVersionInfo ParseFromFile(string jsonPath, string? minecraftVersionFolderPath = null)
    {
        if (!File.Exists(jsonPath))
            throw new ArgumentException("File not exists", nameof(jsonPath));

        var versionInfo = JsonSerializer.Deserialize<MinecraftVersionInfo>(File.ReadAllText(jsonPath),
            MinecraftVersionInfoJsonConverter.Settings);

        if (versionInfo == null)
            throw new InvalidOperationException("Can't parse the version json");

        if (versionInfo.InheritsFrom is not { } inheritsFrom)
            return versionInfo;

        // if this version json don't have inheritsFrom property, this function will end up at there.

        if (minecraftVersionFolderPath == null)
            throw new InvalidOperationException(
                "This version json file have a inheritsFrom property, please provide a minecraft folder path");

        var inheritsFromJsonPath = Path.Join(minecraftVersionFolderPath, inheritsFrom, $"{versionInfo.InheritsFrom}.json");
        if (!File.Exists(inheritsFromJsonPath))
            throw new InvalidOperationException(
                "This version json file have a InheritsFrom property which inherits from a not exists version");

        var inheritsFromVersionInfo = JsonSerializer.Deserialize<MinecraftVersionInfo>(
            File.ReadAllText(inheritsFromJsonPath),
            MinecraftVersionInfoJsonConverter.Settings);

        if (inheritsFromVersionInfo == null)
            throw new InvalidOperationException("Can't parse the inherits from version json");

        // arguments
        versionInfo.LegacyLaunchArguments = InheritsValue(versionInfo.LegacyLaunchArguments,
            inheritsFromVersionInfo.LegacyLaunchArguments);

        if (inheritsFromVersionInfo.ModernLaunchArguments is { } inheritsModernLaunchArguments)
        {
            if (versionInfo.ModernLaunchArguments == null)
            {
                versionInfo.ModernLaunchArguments = inheritsModernLaunchArguments;
            }
            else
            {
                versionInfo.ModernLaunchArguments.GameLaunchArguments = AddLaunchArguments(
                    versionInfo.ModernLaunchArguments.GameLaunchArguments,
                    inheritsModernLaunchArguments.GameLaunchArguments);

                versionInfo.ModernLaunchArguments.JvmLaunchArguments = AddLaunchArguments(
                    versionInfo.ModernLaunchArguments.JvmLaunchArguments,
                    inheritsModernLaunchArguments.JvmLaunchArguments);
            }
        }

        // assets
        versionInfo.AssetIndex = InheritsValue(versionInfo.AssetIndex, inheritsFromVersionInfo.AssetIndex);
        versionInfo.Assets = InheritsValue(versionInfo.Assets, inheritsFromVersionInfo.Assets);

        // java version
        versionInfo.RequiredJavaVersion =
            InheritsValue(versionInfo.RequiredJavaVersion, inheritsFromVersionInfo.RequiredJavaVersion);

        // logging
        versionInfo.LoggingOptions = InheritsValue(versionInfo.LoggingOptions, inheritsFromVersionInfo.LoggingOptions);
        if (versionInfo.LoggingOptions is { ClientLoggingOption: null })
        {
            InheritsValue(versionInfo.LoggingOptions.ClientLoggingOption,
                inheritsFromVersionInfo.LoggingOptions?.ClientLoggingOption);
        }

        // libraries
        var libraries = versionInfo.Libraries.ToList();
        libraries.AddRange(inheritsFromVersionInfo.Libraries);

        versionInfo.Libraries = libraries.ToArray();

        return versionInfo;
    }

    private static TValue? InheritsValue<TValue>(TValue? source, TValue? inheritsFrom)
    {
        return source ?? inheritsFrom;
    }

    private static MinecraftGameLaunchArgument[] AddLaunchArguments(
        IEnumerable<MinecraftGameLaunchArgument>? gameLaunchArguments,
        IEnumerable<MinecraftGameLaunchArgument>? gameLaunchArgumentsToAdd)
    {
        var newGameLaunchArguments = new List<MinecraftGameLaunchArgument>();
        if (gameLaunchArguments != null)
            newGameLaunchArguments.AddRange(gameLaunchArguments);

        if (gameLaunchArgumentsToAdd != null)
            newGameLaunchArguments.AddRange(gameLaunchArgumentsToAdd);

        return newGameLaunchArguments.ToArray();
    }
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
    public MinecraftOptionalRule[]? Rules { get; set; }

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
    [JsonPropertyName("client")] public ClientLoggingOption? ClientLoggingOption { get; set; }
}

public class ClientLoggingOption
{
    [JsonPropertyName("argument")] public string Argument { get; set; }

    [JsonPropertyName("file")] public AssetIndex File { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }
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

internal class GameElementConverter : JsonConverter<MinecraftGameLaunchArgument>
{
    public override bool CanConvert(Type t) => t == typeof(MinecraftGameLaunchArgument);

    public override MinecraftGameLaunchArgument Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                return new MinecraftGameLaunchArgument { String = stringValue };
            case JsonTokenType.StartObject:
                var objectValue = JsonSerializer.Deserialize<MinecraftOptionalLaunchArgument>(ref reader, options);
                return new MinecraftGameLaunchArgument { OptionalLaunchArgument = objectValue };
        }

        throw new Exception("Cannot unmarshal type GameElement");
    }

    public override void Write(Utf8JsonWriter writer, MinecraftGameLaunchArgument value, JsonSerializerOptions options)
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