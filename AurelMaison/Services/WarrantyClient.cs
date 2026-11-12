using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using AurelMaison.Models;

namespace AurelMaison.Services;

public record WarrantyReply(int Status, BaoHanh? Phieu = null);

public sealed class WarrantyClient(HttpClient client, IConfiguration config)
{
    public async Task<WarrantyReply> Tim(string ma, string? cookie, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(cookie)) return new(401);
        if (!Uri.TryCreate(config["Warranty:BaseUrl"], UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https") || baseUri.UserInfo.Length != 0)
            return new(502);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/baohanh/" + Uri.EscapeDataString(ma)));
        request.Headers.TryAddWithoutValidation("Cookie", Security.CookieName + "=" + Uri.EscapeDataString(cookie));
        request.Headers.Accept.ParseAdd("application/xml");
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return new((int)response.StatusCode);
            if (response.Content.Headers.ContentType?.MediaType is not ("application/xml" or "text/xml")) return new(502);
            using var reader = XmlReader.Create(await response.Content.ReadAsStreamAsync(cancellationToken),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 });
            var root = XDocument.Load(reader).Root;
            if (root?.Name != "PhieuBaoHanh" || root.Value("MaPhieu") != ma ||
                !DateOnly.TryParseExact(root.Value("NgayBatDau"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !DateOnly.TryParseExact(root.Value("NgayHetHan"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                return new(502);
            return new(200, new(root.Value("MaPhieu"), root.Value("MaDon"), root.Value("TenMau"),
                root.Value("Serial"), start, end, root.Value("TrangThai")));
        }
        catch (HttpRequestException) { return new(502); }
        catch (XmlException) { return new(502); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(504); }
    }
}
