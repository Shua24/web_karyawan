using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebKaryawan.Core.Data;

namespace WebKaryawan.Tests;

// Memastikan migrasi EF benar-benar bisa membangun skema dari database kosong,
// dan bahwa aplikasi langsung berfungsi di atasnya. Test ini sengaja TIDAK memakai
// PabrikAplikasiUji, karena pabrik itu menyuntikkan koneksi sendiri sehingga
// migrasi tidak pernah dijalankan.
public class MigrasiDatabaseTests
{
    [Fact]
    public async Task Migrasi_MembangunSkemaDariKosongDanAplikasiBerfungsi()
    {
        var jalurBerkas = Path.Combine(Path.GetTempPath(), $"uji_migrasi_{Guid.NewGuid():N}.db");

        try
        {
            var opsiPembangun = new DbContextOptionsBuilder<AplikasiDbContext>()
                .UseSqlite($"Data Source={jalurBerkas}")
                .Options;

            await using (var konteksData = new AplikasiDbContext(opsiPembangun))
            {
                await konteksData.Database.MigrateAsync();

                var migrasiTertunda = await konteksData.Database.GetPendingMigrationsAsync();
                Assert.Empty(migrasiTertunda);

                var migrasiTerpasang = await konteksData.Database.GetAppliedMigrationsAsync();
                Assert.NotEmpty(migrasiTerpasang);
            }

            await using (var koneksi = new SqliteConnection($"Data Source={jalurBerkas}"))
            {
                await koneksi.OpenAsync();

                await using var perintah = koneksi.CreateCommand();
                perintah.CommandText =
                    "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";

                var daftarTabel = new List<string>();
                await using var pembaca = await perintah.ExecuteReaderAsync();

                while (await pembaca.ReadAsync())
                {
                    daftarTabel.Add(pembaca.GetString(0));
                }

                Assert.Contains("Karyawan", daftarTabel);
                Assert.Contains("Negara", daftarTabel);
                Assert.Contains("AspNetUsers", daftarTabel);
                Assert.Contains("AspNetRoles", daftarTabel);
                Assert.Contains("__EFMigrationsHistory", daftarTabel);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            HapusBerkas(jalurBerkas);
        }
    }

    [Fact]
    public async Task AplikasiBerjalanDiAtasDatabaseHasilMigrasi()
    {
        var jalurBerkas = Path.Combine(Path.GetTempPath(), $"uji_app_{Guid.NewGuid():N}.db");

        try
        {
            await using var pabrik = new PabrikAplikasiBerkasUji(jalurBerkas);
            using var klien = pabrik.CreateClient();

            var permintaanMasuk = new
            {
                username = KredensialUji.NamaPenggunaAdmin,
                password = KredensialUji.KataSandiAdmin
            };

            var hasilMasuk = await klien.PostAsJsonAsync("/api/auth/login", permintaanMasuk);
            Assert.Equal(HttpStatusCode.OK, hasilMasuk.StatusCode);

            var halaman = await klien.GetFromJsonAsync<HalamanRingkas>("/api/karyawan");
            Assert.NotNull(halaman);
            Assert.Equal(5, halaman!.TotalBaris);

            var daftarNegara = await klien.GetFromJsonAsync<List<NegaraRingkas>>("/api/negara");
            Assert.NotNull(daftarNegara);
            Assert.Equal(249, daftarNegara!.Count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            HapusBerkas(jalurBerkas);
        }
    }

    private static void HapusBerkas(string jalurBerkas)
    {
        foreach (var akhiran in new[] { "", "-shm", "-wal" })
        {
            var jalur = jalurBerkas + akhiran;
            if (File.Exists(jalur))
            {
                File.Delete(jalur);
            }
        }
    }

    private record HalamanRingkas(int TotalBaris);

    private record NegaraRingkas(string Id, string Nama);
}
