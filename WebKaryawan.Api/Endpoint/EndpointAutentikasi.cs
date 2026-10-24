using Microsoft.AspNetCore.Identity;
using WebKaryawan.Core.Data;

namespace WebKaryawan.Api.Endpoint;

public record PermintaanMasuk(string Username, string Password);
public record PermintaanDaftar(string Username, string Password);
public record GalatKode(string Kode, string Pesan);

public static class EndpointAutentikasi
{
    public static void Daftarkan(WebApplication aplikasi)
    {
        aplikasi.MapPost("/api/auth/login", async (
            PermintaanMasuk permintaan,
            SignInManager<PenggunaAplikasi> pengelolaMasuk) =>
        {
            var hasilMasuk = await pengelolaMasuk.PasswordSignInAsync(
                permintaan.Username,
                permintaan.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!hasilMasuk.Succeeded)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new { pesan = "berhasil" });
        });

        aplikasi.MapPost("/api/auth/daftar", async (
            PermintaanDaftar permintaan,
            UserManager<PenggunaAplikasi> pengelolaPengguna) =>
        {
            var penggunaBaru = new PenggunaAplikasi { UserName = permintaan.Username };
            var hasilBuat = await pengelolaPengguna.CreateAsync(penggunaBaru, permintaan.Password);

            if (!hasilBuat.Succeeded)
            {
                var daftarGalat = hasilBuat.Errors
                    .Select(kegagalan => PetakanGalatIdentity(kegagalan.Code, kegagalan.Description))
                    .ToList();

                return Results.BadRequest(new { galat = daftarGalat });
            }

            await pengelolaPengguna.AddToRoleAsync(penggunaBaru, "User");
            return Results.Ok(new { pesan = "akun dibuat" });
        });

        aplikasi.MapPost("/api/auth/keluar", async (SignInManager<PenggunaAplikasi> pengelolaMasuk) =>
        {
            await pengelolaMasuk.SignOutAsync();
            return Results.Ok(new { pesan = "berhasil keluar" });
        }).RequireAuthorization();

        aplikasi.MapGet("/api/auth/sesi", async (
            HttpContext konteksHttp,
            UserManager<PenggunaAplikasi> pengelolaPengguna) =>
        {
            var pengguna = await pengelolaPengguna.GetUserAsync(konteksHttp.User);

            if (pengguna is null)
            {
                return Results.Unauthorized();
            }

            var daftarPeran = await pengelolaPengguna.GetRolesAsync(pengguna);
            return Results.Ok(new { username = pengguna.UserName, peran = daftarPeran });
        }).RequireAuthorization();
    }

    private static GalatKode PetakanGalatIdentity(string kodeIdentity, string pesanBawaan)
    {
        var kodeKita = kodeIdentity switch
        {
            "DuplicateUserName" => "USERNAME_DIPAKAI",
            "PasswordTooShort" => "PASSWORD_TERLALU_PENDEK",
            "PasswordRequiresDigit" => "PASSWORD_HARUS_ADA_ANGKA",
            "PasswordRequiresNonAlphanumeric" => "PASSWORD_HARUS_ADA_SIMBOL",
            "PasswordRequiresUpper" => "PASSWORD_HARUS_ADA_HURUF_BESAR",
            "PasswordRequiresLower" => "PASSWORD_HARUS_ADA_HURUF_KECIL",
            _ => "AKUN_TIDAK_VALID"
        };

        var pesanKita = kodeKita switch
        {
            "USERNAME_DIPAKAI" => "Nama pengguna sudah dipakai.",
            "PASSWORD_TERLALU_PENDEK" => "Kata sandi minimal 8 karakter.",
            "PASSWORD_HARUS_ADA_ANGKA" => "Kata sandi harus ada angka.",
            "PASSWORD_HARUS_ADA_SIMBOL" => "Kata sandi harus ada simbol.",
            "PASSWORD_HARUS_ADA_HURUF_BESAR" => "Kata sandi harus ada huruf besar.",
            "PASSWORD_HARUS_ADA_HURUF_KECIL" => "Kata sandi harus ada huruf kecil.",
            _ => pesanBawaan
        };

        var galat = new GalatKode(kodeKita, pesanKita);
        return galat;
    }
}
