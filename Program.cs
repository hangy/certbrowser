using System.Security.Cryptography.X509Certificates;

const string file = "/etc/ssl/certs/ca-certificates.crt";
var certificates = new X509Certificate2Collection();
certificates.ImportFromPemFile(file);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

X509Certificate2? FindByThumbprint(string thumbprint) => certificates!.SingleOrDefault(c => c.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase));

static string CertLink(string thumbprint, HttpContext httpContext, string? action)
{
    var request = httpContext.Request;
    return $"{request.Scheme}://{request.Host}/{thumbprint}{(action != null ? $"/{action}" : null)}";
}

static object FakeSerialize(X509Certificate2 certificate, HttpContext httpContext)
{
    _ = certificate ?? throw new ArgumentNullException(nameof(certificate));
    return new { certificate.Subject, certificate.Thumbprint, certificate.FriendlyName, certificate.Issuer, certificate.SerialNumber, _links = new { self = CertLink(certificate.Thumbprint, httpContext, null), download = CertLink(certificate.Thumbprint, httpContext, "download") } };
}

app.MapGet("/", (HttpContext http) => certificates!.Select(c => FakeSerialize(c, http)));
app.MapGet("/{thumbprint}", (string thumbprint, HttpContext http) => FindByThumbprint(thumbprint) is X509Certificate2 cert ? Results.Ok(FakeSerialize(cert, http)) : Results.NotFound());
app.MapGet("/{thumbprint}/download", (string thumbprint) => FindByThumbprint(thumbprint) is X509Certificate2 cert ? Results.File(cert.GetRawCertData(), "application/x-x509-ca-cert", $"{cert.Thumbprint}.crt") : Results.NotFound());

app.Run();
