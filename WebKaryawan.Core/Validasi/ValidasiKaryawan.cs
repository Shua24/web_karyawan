using Microsoft.EntityFrameworkCore;
using WebKaryawan.Core.Data;
using WebKaryawan.Core.Masukan;

namespace WebKaryawan.Core.Validasi;

public static class ValidasiKaryawan
{
    private static readonly string[] PilihanJenisKelamin = ["Laki-laki", "Perempuan"];

    public static async Task<List<GalatValidasi>> ValidasiAsync(
        KaryawanMasukan masukan,
        AplikasiDbContext konteksData,
        string? nikLama,
        string? nikDariRute = null)
    {
        var daftarGalat = new List<GalatValidasi>();

        var nikBersih = (nikDariRute ?? masukan.Nik)?.Trim() ?? string.Empty;

        if (nikBersih.Length == 0)
        {
            daftarGalat.Add(new GalatValidasi("NIK_WAJIB", "NIK wajib diisi."));
        }
        else if (nikBersih.Length != 16 || !nikBersih.All(char.IsAsciiDigit))
        {
            daftarGalat.Add(new GalatValidasi("NIK_TIDAK_VALID", "NIK harus 16 digit angka."));
        }
        else if (nikLama is null && await konteksData.Karyawan.AnyAsync(baris => baris.Nik == nikBersih))
        {
            daftarGalat.Add(new GalatValidasi("NIK_SUDAH_TERDAFTAR", "NIK sudah terdaftar."));
        }

        if (string.IsNullOrWhiteSpace(masukan.NamaLengkap))
        {
            daftarGalat.Add(new GalatValidasi("NAMA_WAJIB", "Nama lengkap wajib diisi."));
        }

        if (string.IsNullOrWhiteSpace(masukan.JenisKelamin)
            || !PilihanJenisKelamin.Contains(masukan.JenisKelamin))
        {
            daftarGalat.Add(new GalatValidasi("JENIS_KELAMIN_TIDAK_VALID", "Jenis kelamin wajib dipilih."));
        }

        if (masukan.TanggalLahir is null)
        {
            daftarGalat.Add(new GalatValidasi("TANGGAL_LAHIR_WAJIB", "Tanggal lahir wajib diisi."));
        }
        else if (masukan.TanggalLahir.Value > DateOnly.FromDateTime(DateTime.Today))
        {
            daftarGalat.Add(new GalatValidasi("TANGGAL_MASA_DEPAN", "Tanggal lahir tidak boleh di masa depan."));
        }

        if (string.IsNullOrWhiteSpace(masukan.Alamat))
        {
            daftarGalat.Add(new GalatValidasi("ALAMAT_WAJIB", "Alamat wajib diisi."));
        }

        if (string.IsNullOrWhiteSpace(masukan.NegaraId))
        {
            daftarGalat.Add(new GalatValidasi("NEGARA_WAJIB", "Negara wajib dipilih."));
        }
        else if (!await konteksData.Negara.AnyAsync(negara => negara.Id == masukan.NegaraId))
        {
            daftarGalat.Add(new GalatValidasi("NEGARA_TIDAK_DIKENAL", "Negara tidak dikenal."));
        }

        return daftarGalat;
    }
}
