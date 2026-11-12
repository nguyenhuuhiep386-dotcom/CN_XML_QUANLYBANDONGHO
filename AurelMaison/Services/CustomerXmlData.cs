using System.Globalization;
using System.Xml.Linq;

namespace AurelMaison.Services;

internal static class XmlData
{
    internal static IEnumerable<XElement> Rows(this XDocument doc) => doc.Root!.Elements();
    internal static string Value(this XElement row, string key) => (string?)row.Element(key) ?? "";
    internal static string? Optional(this XElement row, string key) => (string?)row.Element(key);
    internal static decimal Money(this XElement row, string key) => (decimal)row.Element(key)!;
    internal static XElement? Find(this XDocument doc, string key, string? value) =>
        string.IsNullOrEmpty(value) ? null : doc.Rows().FirstOrDefault(x => x.Value(key).Equals(value, StringComparison.OrdinalIgnoreCase));
    internal static string Iso(DateTime date) => date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    internal static string Next(this XDocument doc, string key, string prefix, int digits)
    {
        var max = doc.Rows().Select(x => x.Value(key)).Where(id => id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(id => int.TryParse(id.AsSpan(prefix.Length), out var n) ? n : 0).DefaultIfEmpty().Max();
        if (max + 1 >= Math.Pow(10, digits)) throw new InvalidOperationException("Đã hết mã cho tiền tố " + prefix);
        return prefix + (max + 1).ToString("D" + digits, CultureInfo.InvariantCulture);
    }

    internal static void SetOptional(this XElement row, string name, string? value, string before)
    {
        if (string.IsNullOrWhiteSpace(value)) row.Element(name)?.Remove();
        else if (row.Element(name) is { } element) element.Value = value.Trim();
        else if (row.Element(before) is { } anchor) anchor.AddBeforeSelf(new XElement(name, value.Trim()));
        else row.Add(new XElement(name, value.Trim()));
    }
}
