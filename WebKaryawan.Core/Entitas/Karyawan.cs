namespace WebKaryawan.Core.Entitas;

public class Karyawan
{
    public string Nik { get; set; } = string.Empty;
    public string NamaLengkap { get; set; } = string.Empty;
    public string JenisKelamin { get; set; } = string.Empty;
    public DateOnly TanggalLahir { get; set; }
    public string Alamat { get; set; } = string.Empty;
    public string NegaraId { get; set; } = string.Empty;
    public Negara? Negara { get; set; }
}
