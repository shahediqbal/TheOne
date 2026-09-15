using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using TheOne.Application.Blog;

namespace TheOne.Application.Website;

public static class WebsiteRules
{
    public static bool Link(string? value) =>
        value is not null && value.Length <= 2000 && !value.Any(char.IsControl) &&
        !value.Contains('\\') &&
        ((value.StartsWith('/') && !value.StartsWith("//")) || BlogRules.SafeUrl(value));

    public static string? Text(WebsiteDocument d, string key) =>
        d.Fields.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    public static Guid? Reference(WebsiteDocument d, string key) =>
        Guid.TryParse(Text(d, key), out var id) ? id : null;

    public static void Validate(string kind, WebsiteDocument d, bool complete)
    {
        var schema = WebsiteSchemas.Get(kind);
        if (d is null || d.Content is null || d.Fields is null || d.Content.Blocks is null)
            throw new ValidationException("Content and fields are required.");

        BlogRules.Validate(d.Content with
        {
            Blocks = d.Content.Blocks.Length == 0 ? [new("Paragraph", d.Content.Title)] : d.Content.Blocks
        });
        if (WebsiteSchemas.Serialize(d).Length > 250000)
            throw new ValidationException("CMS record is too large.");
        if (d.Fields.Keys.Any(k => !schema.Fields.Any(f => f.Key == k)))
            throw new ValidationException("Unknown field for this CMS type.");

        foreach (var f in schema.Fields)
        {
            if (!d.Fields.TryGetValue(f.Key, out var v) || v.ValueKind == JsonValueKind.Null ||
                (v.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(v.GetString())))
            {
                if (complete && f.Required)
                    throw new ValidationException(f.Label + " is required.");
                continue;
            }

            bool valid = f.Type switch
            {
                "text" or "textarea" => v.ValueKind == JsonValueKind.String &&
                    v.GetString()!.Length <= (f.Type == "text" ? 500 : 5000),
                "email" => v.ValueKind == JsonValueKind.String && v.GetString()!.Length <= 254 &&
                    Regex.IsMatch(v.GetString()!, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"),
                "url" => v.ValueKind == JsonValueKind.String && Link(v.GetString()),
                "reference" or "asset" => v.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(v.GetString(), out var id) && id != Guid.Empty,
                "integer" => v.ValueKind == JsonValueKind.Number &&
                    v.TryGetInt32(out var n) && n >= 0 && n <= 100000,
                "latitude" => v.ValueKind == JsonValueKind.Number &&
                    v.TryGetDouble(out var lat) && lat >= -90 && lat <= 90,
                "longitude" => v.ValueKind == JsonValueKind.Number &&
                    v.TryGetDouble(out var lon) && lon >= -180 && lon <= 180,
                "boolean" => v.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "select" => v.ValueKind == JsonValueKind.String && f.Choices!.Contains(v.GetString()),
                "datetime" => v.ValueKind == JsonValueKind.String &&
                    Regex.IsMatch(v.GetString()!, @"(Z|[+-]\d{2}:\d{2})$") &&
                    DateTimeOffset.TryParse(v.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                "posts" => v.ValueKind == JsonValueKind.Array && v.GetArrayLength() <= 200 &&
                    (!complete || v.GetArrayLength() > 0) &&
                    v.EnumerateArray().All(x => x.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(x.GetString(), out var id) && id != Guid.Empty) &&
                    v.EnumerateArray().Select(x => Guid.Parse(x.GetString()!)).Distinct().Count() == v.GetArrayLength(),
                "links" or "menu" => Links(v, f.Type == "menu", complete && f.Required),
                _ => false
            };
            if (!valid)
                throw new ValidationException("Invalid " + f.Label + ".");
        }

        if (kind == "Event" && Text(d, "startsAt") is { } start && Text(d, "endsAt") is { } end &&
            DateTimeOffset.Parse(end, CultureInfo.InvariantCulture) <= DateTimeOffset.Parse(start, CultureInfo.InvariantCulture))
            throw new ValidationException("Event end must be after its start.");

        if (complete && kind == "MediaItem")
        {
            var type = Text(d, "mediaType");
            if (Reference(d, "assetId") is null && Text(d, "url") is null)
                throw new ValidationException("Choose an uploaded image or hosted media URL.");
            if (type != "Photo" && Reference(d, "assetId") is not null)
                throw new ValidationException("Uploaded assets are images; use an HTTPS URL for video/audio.");
        }

        if (complete && kind == "Publication" && Text(d, "availability") == "Available" &&
            !Link(Text(d, "readingUrl")))
            throw new ValidationException("An available publication needs a reading link.");
    }

    private static bool Links(JsonElement value, bool menu, bool required)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 60 ||
            (required && value.GetArrayLength() == 0))
            return false;

        var parents = new Dictionary<string, string?>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Any(p =>
                !(menu ? new[] { "key", "label", "href", "parentKey" } : new[] { "label", "href" }).Contains(p.Name)))
                return false;
            if (!item.TryGetProperty("label", out var label) || label.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(label.GetString()) || label.GetString()!.Length > 100 ||
                !item.TryGetProperty("href", out var href) || href.ValueKind != JsonValueKind.String || !Link(href.GetString()))
                return false;

            if (menu)
            {
                if (!item.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String ||
                    !Regex.IsMatch(key.GetString()!, "^[a-zA-Z0-9_-]{1,50}$") || parents.ContainsKey(key.GetString()!))
                    return false;

                string? parent = null;
                if (item.TryGetProperty("parentKey", out var p) && p.ValueKind != JsonValueKind.Null)
                {
                    if (p.ValueKind != JsonValueKind.String)
                        return false;
                    parent = p.GetString();
                }
                parents[key.GetString()!] = string.IsNullOrEmpty(parent) ? null : parent;
            }
        }

        foreach (var key in parents.Keys)
        {
            var seen = new HashSet<string>();
            string? current = key;
            while (current is not null)
            {
                if (!seen.Add(current) || seen.Count > 4 || !parents.ContainsKey(current))
                    return false;
                current = parents[current];
            }
        }
        return true;
    }
}
