using Microsoft.EntityFrameworkCore;
using WebKaryawan.Core.Data;

namespace WebKaryawan.Api.Endpoint;

public static class EndpointNegara
{
    public static void Daftarkan(WebApplication aplikasi)
    {
        aplikasi.MapGet("/api/negara", async (AplikasiDbContext konteksData) =>
        {
            // Collate NOCASE wajib: collation bawaan SQLite adalah BINARY, sehingga
            // "eSwatini" (huruf kecil) akan terlempar ke urutan paling akhir.
            var daftarNegara = await konteksData.Negara
                .AsNoTracking()
                .OrderBy(negara => EF.Functions.Collate(negara.Nama, "NOCASE"))
                .Select(negara => new { id = negara.Id, nama = negara.Nama })
                .ToListAsync();

            return Results.Ok(daftarNegara);
        }).RequireAuthorization();
    }
}
