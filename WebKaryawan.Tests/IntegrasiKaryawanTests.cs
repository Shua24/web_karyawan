using System.Net;
using System.Net.Http.Json;

namespace WebKaryawan.Tests;

public class IntegrasiKaryawanTests : IClassFixture<PabrikAplikasiUji>
{
    private readonly PabrikAplikasiUji _pabrik;

    public IntegrasiKaryawanTests(PabrikAplikasiUji pabrik)
    {
        _pabrik = pabrik;
    }

    private async Task<HttpClient> BuatKlienAdminAsync()
    {
        var klien = _pabrik.CreateClient();
        var permintaanMasuk = new
        {
            username = KredensialUji.NamaPenggunaAdmin,
            password = KredensialUji.KataSandiAdmin
        };

        var hasilMasuk = await klien.PostAsJsonAsync("/api/auth/login", permintaanMasuk);
        Assert.Equal(HttpStatusCode.OK, hasilMasuk.StatusCode);
        return klien;
    }

    private async Task<HttpClient> BuatKlienPenggunaBiasaAsync()
    {
        var namaPengguna = "user" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var permintaanDaftar = new
        {
            username = namaPengguna,
            password = KredensialUji.KataSandiPenggunaBiasa
        };

        using var klienPendaftar = _pabrik.CreateClient();
        var hasilDaftar = await klienPendaftar.PostAsJsonAsync("/api/auth/daftar", permintaanDaftar);
        Assert.Equal(HttpStatusCode.OK, hasilDaftar.StatusCode);

        var klien = _pabrik.CreateClient();
        var permintaanMasuk = new
        {
            username = namaPengguna,
            password = KredensialUji.KataSandiPenggunaBiasa
        };

        var hasilMasuk = await klien.PostAsJsonAsync("/api/auth/login", permintaanMasuk);
        Assert.Equal(HttpStatusCode.OK, hasilMasuk.StatusCode);
        return klien;
    }

    [Fact]
    public async Task TanpaLogin_Ditolak401()
    {
        using var klien = _pabrik.CreateClient();

        var tanggapan = await klien.GetAsync("/api/karyawan");

        Assert.Equal(HttpStatusCode.Unauthorized, tanggapan.StatusCode);
    }

    [Fact]
    public async Task SetelahLogin_SeedTerbacaUmurTerhitungDanUrutNik()
    {
        using var klien = await BuatKlienAdminAsync();

        var halaman = await klien.GetFromJsonAsync<HalamanKaryawan>("/api/karyawan");

        Assert.NotNull(halaman);
        Assert.Equal(5, halaman!.TotalBaris);

        var messi = halaman.Daftar.Single(baris => baris.NamaLengkap == "Lionel Messi");
        Assert.Equal("Argentina", messi.Negara);
        Assert.Equal("1987-01-01", messi.TanggalLahir);
        Assert.InRange(messi.Umur, 38, 40);

        var daftarNik = halaman.Daftar.Select(baris => baris.Nik).ToList();
        var daftarNikTerurut = daftarNik.OrderBy(nik => nik, StringComparer.Ordinal).ToList();
        Assert.Equal(daftarNikTerurut, daftarNik);
    }

    [Fact]
    public async Task CariNikDanNama_DigabungAnd()
    {
        using var klien = await BuatKlienAdminAsync();

        var cocokDua = await klien.GetFromJsonAsync<HalamanKaryawan>("/api/karyawan?nik=3171&nama=Messi");
        Assert.NotNull(cocokDua);
        Assert.Equal(1, cocokDua!.TotalBaris);

        var nikCocokNamaTidak = await klien.GetFromJsonAsync<HalamanKaryawan>(
            "/api/karyawan?nik=317101010187&nama=Ronaldo");
        Assert.NotNull(nikCocokNamaTidak);
        Assert.Equal(0, nikCocokNamaTidak!.TotalBaris);
    }

    [Fact]
    public async Task Paging_MembatasiDanMelaporkanTotal()
    {
        using var klien = await BuatKlienAdminAsync();

        var halaman = await klien.GetFromJsonAsync<HalamanKaryawan>("/api/karyawan?halaman=1&ukuranHalaman=2");

        Assert.NotNull(halaman);
        Assert.Equal(5, halaman!.TotalBaris);
        Assert.Equal(2, halaman.Daftar.Count);
        Assert.Equal(1, halaman.NomorHalaman);
    }

    [Fact]
    public async Task TambahTanpaNik16Digit_MenolakDenganKodeTerstruktur()
    {
        using var klien = await BuatKlienAdminAsync();

        var masukan = new
        {
            nik = "123",
            namaLengkap = "Budi",
            jenisKelamin = "Laki-laki",
            tanggalLahir = "1990-05-05",
            alamat = "Jl. Mawar 1",
            negaraId = "ID"
        };

        var tanggapan = await klien.PostAsJsonAsync("/api/karyawan", masukan);

        Assert.Equal(HttpStatusCode.BadRequest, tanggapan.StatusCode);
        var isi = await tanggapan.Content.ReadFromJsonAsync<BadanGalat>();
        Assert.NotNull(isi);
        Assert.Contains(isi!.Galat, galat => galat.Kode == "NIK_TIDAK_VALID");
    }

    [Fact]
    public async Task TambahNikDuplikat_MenolakDenganKodeNikSudahTerdaftar()
    {
        using var klien = await BuatKlienAdminAsync();

        var masukan = new
        {
            nik = "3171010101870001",
            namaLengkap = "Duplikat",
            jenisKelamin = "Laki-laki",
            tanggalLahir = "1990-05-05",
            alamat = "Jl. Mawar 1",
            negaraId = "ID"
        };

        var tanggapan = await klien.PostAsJsonAsync("/api/karyawan", masukan);

        Assert.Equal(HttpStatusCode.BadRequest, tanggapan.StatusCode);
        var isi = await tanggapan.Content.ReadFromJsonAsync<BadanGalat>();
        Assert.NotNull(isi);
        Assert.Contains(isi!.Galat, galat => galat.Kode == "NIK_SUDAH_TERDAFTAR");
    }

    [Fact]
    public async Task AlurLengkap_TambahUbahBacaHapus()
    {
        using var klien = await BuatKlienAdminAsync();
        const string nik = "3171010101990009";

        var masukanTambah = new
        {
            nik,
            namaLengkap = "Uji Alur Lengkap",
            jenisKelamin = "Laki-laki",
            tanggalLahir = "1999-09-09",
            alamat = "Jl. Kenanga 7",
            negaraId = "ID"
        };

        var hasilTambah = await klien.PostAsJsonAsync("/api/karyawan", masukanTambah);
        Assert.Equal(HttpStatusCode.Created, hasilTambah.StatusCode);

        var masukanUbah = new
        {
            namaLengkap = "Uji Alur Lengkap Jaya",
            jenisKelamin = "Laki-laki",
            tanggalLahir = "1999-09-09",
            alamat = "Jl. Kenanga 9",
            negaraId = "MY"
        };

        var hasilUbah = await klien.PutAsJsonAsync($"/api/karyawan/{nik}", masukanUbah);
        Assert.Equal(HttpStatusCode.OK, hasilUbah.StatusCode);

        var detail = await klien.GetFromJsonAsync<BarisKaryawan>($"/api/karyawan/{nik}");
        Assert.NotNull(detail);
        Assert.Equal("Uji Alur Lengkap Jaya", detail!.NamaLengkap);
        Assert.Equal("Malaysia", detail.Negara);

        var hasilHapus = await klien.DeleteAsync($"/api/karyawan/{nik}");
        Assert.Equal(HttpStatusCode.OK, hasilHapus.StatusCode);

        var setelahHapus = await klien.GetAsync($"/api/karyawan/{nik}");
        Assert.Equal(HttpStatusCode.NotFound, setelahHapus.StatusCode);
    }

    [Fact]
    public async Task UserBiasa_BolehBacaTapiDitolakSaatMenulis()
    {
        using var klien = await BuatKlienPenggunaBiasaAsync();

        var hasilBaca = await klien.GetAsync("/api/karyawan");
        Assert.Equal(HttpStatusCode.OK, hasilBaca.StatusCode);

        var masukan = new
        {
            nik = "3171010102000007",
            namaLengkap = "Penyusup",
            jenisKelamin = "Laki-laki",
            tanggalLahir = "2000-01-01",
            alamat = "Jl. Nangka 2",
            negaraId = "ID"
        };

        var hasilTulis = await klien.PostAsJsonAsync("/api/karyawan", masukan);
        Assert.Equal(HttpStatusCode.Forbidden, hasilTulis.StatusCode);

        var hasilHapus = await klien.DeleteAsync("/api/karyawan/3171010101870001");
        Assert.Equal(HttpStatusCode.Forbidden, hasilHapus.StatusCode);
    }

    [Fact]
    public async Task DaftarAkunBaru_MasukRoleUserBukanAdmin()
    {
        using var klien = await BuatKlienPenggunaBiasaAsync();

        var sesi = await klien.GetFromJsonAsync<SesiPengguna>("/api/auth/sesi");

        Assert.NotNull(sesi);
        Assert.Contains("User", sesi!.Peran);
        Assert.DoesNotContain("Admin", sesi.Peran);
    }

    [Fact]
    public async Task Keluar_SetelahKeluarDitolak401()
    {
        using var klien = await BuatKlienAdminAsync();

        var hasilKeluar = await klien.PostAsync("/api/auth/keluar", null);
        Assert.Equal(HttpStatusCode.OK, hasilKeluar.StatusCode);

        var hasilBaca = await klien.GetAsync("/api/karyawan");
        Assert.Equal(HttpStatusCode.Unauthorized, hasilBaca.StatusCode);
    }

    [Fact]
    public async Task TanggalLahirMasaDepan_Ditolak()
    {
        using var klien = await BuatKlienAdminAsync();

        var masukan = new
        {
            nik = "3171010103990008",
            namaLengkap = "Belum Lahir",
            jenisKelamin = "Perempuan",
            tanggalLahir = "2999-01-01",
            alamat = "Jl. Antah 1",
            negaraId = "ID"
        };

        var tanggapan = await klien.PostAsJsonAsync("/api/karyawan", masukan);

        Assert.Equal(HttpStatusCode.BadRequest, tanggapan.StatusCode);
        var isi = await tanggapan.Content.ReadFromJsonAsync<BadanGalat>();
        Assert.NotNull(isi);
        Assert.Contains(isi!.Galat, galat => galat.Kode == "TANGGAL_MASA_DEPAN");
    }

    private record HalamanKaryawan(
        int TotalBaris,
        int NomorHalaman,
        int UkuranHalaman,
        List<BarisKaryawan> Daftar);

    private record BarisKaryawan(
        string Nik,
        string NamaLengkap,
        string JenisKelamin,
        string TanggalLahir,
        string Alamat,
        string NegaraId,
        string Negara,
        int Umur);

    private record BadanGalat(List<GalatItem> Galat);

    private record GalatItem(string Kode, string Pesan);

    private record SesiPengguna(string Username, List<string> Peran);
}
