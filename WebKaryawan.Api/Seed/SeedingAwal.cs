using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebKaryawan.Core.Data;
using WebKaryawan.Core.Entitas;

namespace WebKaryawan.Api.Seed;

public static class SeedingAwal
{
    public static async Task JalankanAsync(WebApplication aplikasi)
    {
        using var lingkup = aplikasi.Services.CreateScope();
        var penyediaLayanan = lingkup.ServiceProvider;

        var konteksData = penyediaLayanan.GetRequiredService<AplikasiDbContext>();
        await konteksData.Database.EnsureCreatedAsync();

        await SeedNegaraAsync(konteksData, aplikasi.Environment.ContentRootPath);
        await SeedAkunAsync(penyediaLayanan, aplikasi.Configuration);
        await SeedKaryawanAsync(konteksData);
    }

    private static async Task SeedNegaraAsync(AplikasiDbContext konteksData, string akarKonten)
    {
        var negaraSudahAda = await konteksData.Negara.AnyAsync();
        if (negaraSudahAda)
        {
            return;
        }

        var jalurBerkas = Path.Combine(akarKonten, "Data", "negara-id.json");
        var teksBerkas = await File.ReadAllTextAsync(jalurBerkas);

        var opsiBaca = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var daftarNegara = JsonSerializer.Deserialize<List<Negara>>(teksBerkas, opsiBaca);

        if (daftarNegara is null || daftarNegara.Count == 0)
        {
            return;
        }

        konteksData.Negara.AddRange(daftarNegara);
        await konteksData.SaveChangesAsync();
    }

    private static async Task SeedAkunAsync(IServiceProvider penyediaLayanan, IConfiguration konfigurasi)
    {
        var pengelolaPeran = penyediaLayanan.GetRequiredService<RoleManager<IdentityRole>>();
        var pengelolaPengguna = penyediaLayanan.GetRequiredService<UserManager<PenggunaAplikasi>>();

        foreach (var namaPeran in new[] { "Admin", "User" })
        {
            var peranAda = await pengelolaPeran.RoleExistsAsync(namaPeran);
            if (!peranAda)
            {
                await pengelolaPeran.CreateAsync(new IdentityRole(namaPeran));
            }
        }

        var namaPenggunaAdmin = konfigurasi["AkunAdmin:NamaPengguna"];
        var kataSandiAdmin = konfigurasi["AkunAdmin:KataSandi"];

        if (string.IsNullOrWhiteSpace(namaPenggunaAdmin) || string.IsNullOrWhiteSpace(kataSandiAdmin))
        {
            throw new InvalidOperationException(
                "AkunAdmin:NamaPengguna dan AkunAdmin:KataSandi wajib diisi di appsettings.Development.json.");
        }

        var adminSudahAda = await pengelolaPengguna.FindByNameAsync(namaPenggunaAdmin);
        if (adminSudahAda is not null)
        {
            return;
        }

        var penggunaAdmin = new PenggunaAplikasi { UserName = namaPenggunaAdmin };
        var hasilBuat = await pengelolaPengguna.CreateAsync(penggunaAdmin, kataSandiAdmin);

        if (!hasilBuat.Succeeded)
        {
            var daftarPesan = string.Join("; ", hasilBuat.Errors.Select(galat => galat.Description));
            throw new InvalidOperationException($"Gagal membuat akun admin: {daftarPesan}");
        }

        await pengelolaPengguna.AddToRoleAsync(penggunaAdmin, "Admin");
    }

    private static async Task SeedKaryawanAsync(AplikasiDbContext konteksData)
    {
        var karyawanSudahAda = await konteksData.Karyawan.AnyAsync();
        if (karyawanSudahAda)
        {
            return;
        }

        konteksData.Karyawan.AddRange(
            new Karyawan
            {
                Nik = "3171010101870001",
                NamaLengkap = "Lionel Messi",
                JenisKelamin = "Laki-laki",
                TanggalLahir = new DateOnly(1987, 1, 1),
                Alamat = "Jl. Achmad Yani No 89 Jakarta Pusat",
                NegaraId = "AR"
            },
            new Karyawan
            {
                Nik = "3171010105850002",
                NamaLengkap = "Cristiano Ronaldo",
                JenisKelamin = "Laki-laki",
                TanggalLahir = new DateOnly(1985, 2, 5),
                Alamat = "Jl. Sudirman No 12 Jakarta Selatan",
                NegaraId = "PT"
            },
            new Karyawan
            {
                Nik = "3171010109730003",
                NamaLengkap = "Rambang Pamungkas",
                JenisKelamin = "Laki-laki",
                TanggalLahir = new DateOnly(1973, 12, 1),
                Alamat = "Jl. Gatot Subroto No 21 Jakarta Selatan",
                NegaraId = "ID"
            },
            new Karyawan
            {
                Nik = "3171010109010004",
                NamaLengkap = "Natassha Romanov",
                JenisKelamin = "Perempuan",
                TanggalLahir = new DateOnly(2001, 1, 1),
                Alamat = "Jl. Melati No 3 Jakarta Timur",
                NegaraId = "RU"
            },
            new Karyawan
            {
                Nik = "3171010109960005",
                NamaLengkap = "Ertis Siti Julaidah",
                JenisKelamin = "Perempuan",
                TanggalLahir = new DateOnly(1996, 6, 15),
                Alamat = "Jl. Ampang No 8 Jakarta Pusat",
                NegaraId = "MY"
            });

        await konteksData.SaveChangesAsync();
    }
}
