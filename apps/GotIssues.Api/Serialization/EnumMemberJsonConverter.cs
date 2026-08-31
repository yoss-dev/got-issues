using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GotIssues.Api.Serialization;

/// <summary>
/// Serialises generated contract enums using the values the specification declares.
///
/// <para>
/// The generator emits <c>[EnumMember(Value = "in_progress")]</c> on each member — an
/// attribute Newtonsoft honours and <c>System.Text.Json</c> ignores. This project
/// generates with <c>useNewtonsoft=false</c>, so without this converter the API would
/// serialise <c>status</c> as <c>2</c> while <c>spec/openapi.yaml</c> declares
/// <c>enum: [open, in_progress, done]</c> — the document promising one thing and the
/// API sending another, which is this repository's signature defect.
/// </para>
/// <para>
/// Found by T-0006's own tests, which are the first to exercise an enum in the contract.
/// A plain <see cref="JsonStringEnumConverter"/> would not do: the generated member
/// names are <c>OpenEnum</c> and <c>InProgressEnum</c>, so it would emit those.
/// </para>
/// </summary>
public sealed class EnumMemberJsonConverter : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter?> Cache = new();

    /// <summary>
    /// Claims the enum itself and never <c>Nullable&lt;T&gt;</c>.
    ///
    /// System.Text.Json wraps a converter for <c>T</c> to serve <c>T?</c> on its own.
    /// Claiming the nullable type and returning a <c>JsonConverter&lt;T&gt;</c> instead
    /// makes it throw, which surfaces as a 500 on a request that should be a 400 — the
    /// first version of this class did exactly that, and the enum-rejection tests caught
    /// it.
    /// </summary>
    public override bool CanConvert(Type typeToConvert) =>
        Cache.GetOrAdd(typeToConvert, Build) is not null;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        Cache.GetOrAdd(typeToConvert, Build);

    /// <summary>
    /// Builds a converter only for enums that actually declare member values. An enum
    /// without them is left to the framework rather than silently given new behaviour.
    /// </summary>
    private static JsonConverter? Build(Type type)
    {
        if (!type.IsEnum)
        {
            return null;
        }

        var named = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (Field: f, Attribute: f.GetCustomAttribute<EnumMemberAttribute>()))
            .Where(pair => pair.Attribute?.Value is not null)
            .ToList();

        if (named.Count == 0)
        {
            return null;
        }

        var converterType = typeof(NamedEnumConverter<>).MakeGenericType(type);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class NamedEnumConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        private static readonly Dictionary<string, TEnum> ByName =
            typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.GetCustomAttribute<EnumMemberAttribute>()?.Value is not null)
                .ToDictionary(
                    f => f.GetCustomAttribute<EnumMemberAttribute>()!.Value!,
                    f => (TEnum)f.GetValue(null)!,
                    StringComparer.Ordinal);

        private static readonly Dictionary<TEnum, string> ByValue =
            ByName.ToDictionary(pair => pair.Value, pair => pair.Key);

        public override TEnum Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

            // An unknown value throws, which model binding turns into a 400 problem
            // document naming the field — the contract rejecting the request, rather
            // than a controller checking a set it re-derived.
            return value is not null && ByName.TryGetValue(value, out var parsed)
                ? parsed
                : throw new JsonException(
                    $"'{value}' is not one of: {string.Join(", ", ByName.Keys)}.");
        }

        public override void Write(
            Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);

            if (!ByValue.TryGetValue(value, out var name))
            {
                throw new JsonException($"{typeof(TEnum).Name} value {value} declares no contract name.");
            }

            writer.WriteStringValue(name);
        }
    }
}
