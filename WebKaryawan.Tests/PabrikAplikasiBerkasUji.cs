using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebKaryawan.Core.Data;

namespace WebKaryawan.Tests;

// Sama seperti PabrikAplikasiUji, tapi memakai berkas SQLite sungguhan alih-alih
// in-memory, supaya jalur migrasi (Database.MigrateAsync) benar-benar dijalankan
// oleh SeedingAwal saat aplikasi start.
public class PabrikAplikasiBerkasUji : WebApplicationFactory<Program>
{
    private readonly string jalurBerkas;

    public PabrikAplikasiBerkasUji(string jalurBerkas)
    {
        this.jalurBerkas = jalurBerkas;
    }

    protected override void ConfigureWebHost(IWebHostBuilder pembangun)
    {
        pembangun.ConfigureAppConfiguration((konteks, konfigurasi) =>
        {
            var nilaiUji = new Dictionary<string, string?>
            {
                ["AkunAdmin:NamaPengguna"] = KredensialUji.NamaPenggunaAdmin,
                ["AkunAdmin:KataSandi"] = KredensialUji.KataSandiAdmin,
                ["ConnectionStrings:Bawaan"] = $"Data Source={jalurBerkas}"
            };

            konfigurasi.AddInMemoryCollection(nilaiUji);
        });

        pembangun.ConfigureServices(layanan =>
        {
            // Buang DbContext yang didaftarkan Program.cs, lalu daftarkan ulang
            // dengan berkas SQLite uji ini.
            layanan.RemoveAll<DbContextOptions<AplikasiDbContext>>();
            layanan.AddDbContext<AplikasiDbContext>(
                opsi => opsi.UseSqlite($"Data Source={jalurBerkas}"));
        });
    }
}
