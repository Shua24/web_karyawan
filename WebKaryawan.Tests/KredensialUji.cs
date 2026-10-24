namespace WebKaryawan.Tests;

// Kredensial khusus test. Nilainya sengaja BERBEDA dari appsettings.Development.json
// supaya test tidak bergantung pada konfigurasi lokal dan tidak ada rahasia dev
// yang ikut masuk ke repositori.
public static class KredensialUji
{
    public const string NamaPenggunaAdmin = "admin-uji";
    public const string KataSandiAdmin = "KataSandiUji123!";
    public const string KataSandiPenggunaBiasa = "KataSandiUji123!";
}
