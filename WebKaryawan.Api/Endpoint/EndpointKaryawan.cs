using Microsoft.EntityFrameworkCore;
using WebKaryawan.Core.Data;
using WebKaryawan.Core.Entitas;
using WebKaryawan.Core.Masukan;
using WebKaryawan.Core.Validasi;

namespace WebKaryawan.Api.Endpoint;

public static class EndpointKaryawan
{
    public static void Daftarkan(WebApplication aplikasi)
    {
        aplikasi.MapGet("/api/karyawan", async (
            AplikasiDbContext konteksData,
            string? nik,
            string? nama,
            int? halaman,
            int? ukuranHalaman) =>
        {
            var ukuran = ukuranHalaman ?? 10;
            var nomorHalaman = halaman ?? 1;

            if (nomorHalaman < 1)
            {
                nomorHalaman = 1;
            }

            var kueri = konteksData.Karyawan.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(nik))
            {
                var polaNik = "%" + nik.Trim() + "%";
                kueri = kueri.Where(karyawan => EF.Functions.Like(karyawan.Nik, polaNik));
            }

            if (!string.IsNullOrWhiteSpace(nama))
            {
                var polaNama = "%" + nama.Trim() + "%";
                kueri = kueri.Where(karyawan => EF.Functions.Like(karyawan.NamaLengkap, polaNama));
            }

            var totalBaris = await kueri.CountAsync();
            var hariIni = DateOnly.FromDateTime(DateTime.Today);

            var daftarKaryawan = await kueri
                .OrderBy(karyawan => karyawan.Nik)
                .Skip((nomorHalaman - 1) * ukuran)
                .Take(ukuran)
                .Select(karyawan => new
                {
                    nik = karyawan.Nik,
                    namaLengkap = karyawan.NamaLengkap,
                    jenisKelamin = karyawan.JenisKelamin,
                    tanggalLahir = karyawan.TanggalLahir,
                    alamat = karyawan.Alamat,
                    negara = karyawan.Negara!.Nama,
                    umur = hariIni.Year - karyawan.TanggalLahir.Year
                        - (hariIni.DayOfYear < karyawan.TanggalLahir.DayOfYear ? 1 : 0)
                })
                .ToListAsync();

            var hasilHalaman = new
            {
                totalBaris,
                nomorHalaman,
                ukuranHalaman = ukuran,
                daftar = daftarKaryawan
            };

            return Results.Ok(hasilHalaman);
        }).RequireAuthorization();

        aplikasi.MapGet("/api/karyawan/{nik}", async (string nik, AplikasiDbContext konteksData) =>
        {
            var hariIni = DateOnly.FromDateTime(DateTime.Today);

            var karyawan = await konteksData.Karyawan
                .AsNoTracking()
                .Where(baris => baris.Nik == nik)
                .Select(baris => new
                {
                    nik = baris.Nik,
                    namaLengkap = baris.NamaLengkap,
                    jenisKelamin = baris.JenisKelamin,
                    tanggalLahir = baris.TanggalLahir,
                    alamat = baris.Alamat,
                    negaraId = baris.NegaraId,
                    negara = baris.Negara!.Nama,
                    umur = hariIni.Year - baris.TanggalLahir.Year
                        - (hariIni.DayOfYear < baris.TanggalLahir.DayOfYear ? 1 : 0)
                })
                .FirstOrDefaultAsync();

            if (karyawan is null)
            {
                return Results.NotFound(GalatTidakDitemukan());
            }

            return Results.Ok(karyawan);
        }).RequireAuthorization();

        aplikasi.MapPost("/api/karyawan", async (
            KaryawanMasukan masukan,
            AplikasiDbContext konteksData) =>
        {
            var daftarGalat = await ValidasiKaryawan.ValidasiAsync(masukan, konteksData, nikLama: null);

            if (daftarGalat.Count > 0)
            {
                return Results.BadRequest(new { galat = daftarGalat });
            }

            var karyawanBaru = new Karyawan
            {
                Nik = masukan.Nik!.Trim(),
                NamaLengkap = masukan.NamaLengkap!,
                JenisKelamin = masukan.JenisKelamin!,
                TanggalLahir = masukan.TanggalLahir!.Value,
                Alamat = masukan.Alamat!,
                NegaraId = masukan.NegaraId!
            };

            konteksData.Karyawan.Add(karyawanBaru);
            await konteksData.SaveChangesAsync();

            return Results.Created($"/api/karyawan/{karyawanBaru.Nik}", new { nik = karyawanBaru.Nik });
        }).RequireAuthorization(opsi => opsi.RequireRole("Admin"));

        aplikasi.MapPut("/api/karyawan/{nik}", async (
            string nik,
            KaryawanMasukan masukan,
            AplikasiDbContext konteksData) =>
        {
            var karyawanLama = await konteksData.Karyawan.FindAsync(nik);

            if (karyawanLama is null)
            {
                return Results.NotFound(GalatTidakDitemukan());
            }

            // NIK read-only saat ubah, jadi nilainya diambil dari rute, bukan dari body.
            var daftarGalat = await ValidasiKaryawan.ValidasiAsync(
                masukan,
                konteksData,
                nikLama: nik,
                nikDariRute: nik);

            if (daftarGalat.Count > 0)
            {
                return Results.BadRequest(new { galat = daftarGalat });
            }

            karyawanLama.NamaLengkap = masukan.NamaLengkap!;
            karyawanLama.JenisKelamin = masukan.JenisKelamin!;
            karyawanLama.TanggalLahir = masukan.TanggalLahir!.Value;
            karyawanLama.Alamat = masukan.Alamat!;
            karyawanLama.NegaraId = masukan.NegaraId!;

            await konteksData.SaveChangesAsync();
            return Results.Ok(new { nik = karyawanLama.Nik });
        }).RequireAuthorization(opsi => opsi.RequireRole("Admin"));

        aplikasi.MapDelete("/api/karyawan/{nik}", async (string nik, AplikasiDbContext konteksData) =>
        {
            var karyawan = await konteksData.Karyawan.FindAsync(nik);

            if (karyawan is null)
            {
                return Results.NotFound(GalatTidakDitemukan());
            }

            konteksData.Karyawan.Remove(karyawan);
            await konteksData.SaveChangesAsync();

            return Results.Ok(new { pesan = "data dihapus" });
        }).RequireAuthorization(opsi => opsi.RequireRole("Admin"));
    }

    private static object GalatTidakDitemukan()
    {
        var daftarGalat = new[]
        {
            new GalatValidasi("TIDAK_DITEMUKAN", "Data tidak ditemukan.")
        };

        return new { galat = daftarGalat };
    }
}
