using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebKaryawan.Api.Endpoint;
using WebKaryawan.Api.Seed;
using WebKaryawan.Core.Data;

var builder = WebApplication.CreateBuilder(args);

var teksKoneksi = builder.Configuration.GetConnectionString("Bawaan")
    ?? "Data Source=webkaryawan.db";

builder.Services.AddDbContext<AplikasiDbContext>(
    opsi => opsi.UseSqlite(teksKoneksi));

builder.Services
    .AddIdentity<PenggunaAplikasi, IdentityRole>(opsi =>
    {
        opsi.Password.RequiredLength = 8;
        opsi.User.RequireUniqueEmail = false;
        opsi.Lockout.MaxFailedAccessAttempts = 5;
        opsi.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AplikasiDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opsi =>
{
    opsi.Cookie.HttpOnly = true;
    opsi.Cookie.SameSite = SameSiteMode.Lax;
    opsi.LoginPath = "/login.html";

    // Endpoint API harus menjawab 401, bukan 302 ke halaman login.
    opsi.Events.OnRedirectToLogin = konteks =>
    {
        if (konteks.Request.Path.StartsWithSegments("/api"))
        {
            konteks.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        konteks.Response.Redirect(konteks.RedirectUri);
        return Task.CompletedTask;
    };

    // Tanpa ini, penolakan role menjadi 302 ke /Account/AccessDenied lalu 404,
    // bukan 403 seperti yang diharapkan klien API.
    opsi.Events.OnRedirectToAccessDenied = konteks =>
    {
        if (konteks.Request.Path.StartsWithSegments("/api"))
        {
            konteks.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        konteks.Response.Redirect(konteks.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

var aplikasi = builder.Build();

await SeedingAwal.JalankanAsync(aplikasi);

aplikasi.UseDefaultFiles();
aplikasi.UseStaticFiles();
aplikasi.UseAuthentication();
aplikasi.UseAuthorization();

EndpointAutentikasi.Daftarkan(aplikasi);
EndpointNegara.Daftarkan(aplikasi);
EndpointKaryawan.Daftarkan(aplikasi);

aplikasi.Run();

public partial class Program
{
}
