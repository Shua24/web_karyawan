using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebKaryawan.Core.Data;

namespace WebKaryawan.Tests;

public class PabrikAplikasiUji : WebApplicationFactory<Program>
{
    private readonly SqliteConnection koneksiUji = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder pembangun)
    {
        koneksiUji.Open();

        // Kredensial admin untuk test disuntikkan di sini, bukan diwarisi dari
        // appsettings.Development.json. Test jadi tidak bergantung pada konfigurasi
        // lokal dan tidak ada rahasia dev yang perlu ditulis di repositori.
        pembangun.ConfigureAppConfiguration((konteks, konfigurasi) =>
        {
            var nilaiUji = new Dictionary<string, string?>
            {
                ["AkunAdmin:NamaPengguna"] = KredensialUji.NamaPenggunaAdmin,
                ["AkunAdmin:KataSandi"] = KredensialUji.KataSandiAdmin
            };

            konfigurasi.AddInMemoryCollection(nilaiUji);
        });

        pembangun.ConfigureServices(layanan =>
        {
            layanan.RemoveAll<DbContextOptions<AplikasiDbContext>>();
            layanan.AddDbContext<AplikasiDbContext>(
                opsi => opsi.UseSqlite(koneksiUji));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        koneksiUji.Dispose();
    }
}
