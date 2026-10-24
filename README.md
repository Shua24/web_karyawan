# Web Karyawan — Aplikasi Data Pribadi

Aplikasi CRUD data pribadi karyawan: NIK, nama lengkap, jenis kelamin, tanggal lahir, alamat,
dan negara. Back-end ASP.NET Core Web API, front-end HTML + jQuery + Bootstrap statis, database
SQLite, autentikasi ASP.NET Core Identity berbasis cookie.

Dibuat untuk memenuhi spesifikasi teknis: front-end HTML/JavaScript/CSS dengan jQuery dan
Bootstrap, back-end C# (ASP.NET). **Tidak memakai Blazor dan tidak memakai Razor** — halaman
adalah berkas statis yang memanggil API lewat jQuery.

**Status:** semua klaim di dokumen ini diverifikasi langsung di mesin ini. Build bersih
(0 warning, 0 error), 13 test lulus, dan alur CRUD diuji lewat HTTP serta browser.

---

## Daftar Isi

- [Untuk Pengguna Aplikasi](#untuk-pengguna-aplikasi)
- [Kebutuhan Sistem](#kebutuhan-sistem)
- [Mulai Cepat](#mulai-cepat)
- [Struktur Repositori](#struktur-repositori)
- [Arsitektur](#arsitektur)
- [Referensi API](#referensi-api)
- [Model Data](#model-data)
- [Aturan Validasi](#aturan-validasi)
- [Menjalankan Test](#menjalankan-test)
- [Migrasi Database](#migrasi-database)
- [Jebakan yang Wajib Diketahui](#jebakan-yang-wajib-diketahui)
- [Cara Memperluas](#cara-memperluas)
- [Yang Belum Ada](#yang-belum-ada)
- [Lisensi](#lisensi)

---

## Untuk Pengguna Aplikasi

Ada dua peran. Akun **Admin** hanya dibuat lewat seeding; akun **User** dibuat sendiri lewat
halaman daftar.

| Peran | Bisa lihat data | Tambah | Ubah | Hapus |
|---|---|---|---|---|
| `Admin` | ya | ya | ya | ya |
| `User` | ya | tidak | tidak | tidak |

Alur sehari-hari:

1. **Masuk** di `/login.html`. Kalau belum punya akun, daftar di `/daftar.html` — akun baru
   otomatis masuk peran `User` (hanya bisa melihat).
2. **Monitoring** di halaman utama: tabel data dengan kolom No, NIK, Nama Lengkap, Umur,
   Tanggal Lahir, Jenis Kelamin, Alamat, Negara, dan Aksi.
3. **Mencari** lewat panel pencarian: isi NIK dan/atau Nama, lalu tekan Cari. Kedua isian
   digabung dengan AND, dan pencocokannya mengandung (`LIKE %kata%`), bukan persis.
4. **Tambah** data lewat tombol Tambah. Semua kolom wajib diisi.
5. **Detail** membuka modal read-only. **Ubah** membuka formulir — NIK tidak bisa diubah.
6. **Hapus** selalu meminta konfirmasi lebih dulu.

---

## Kebutuhan Sistem

| Kebutuhan | Versi terverifikasi |
|---|---|
| .NET SDK | 10.0.402 |
| Runtime ASP.NET Core | 10.0.13 |
| SQLite | lewat `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 (tidak perlu instalasi terpisah) |
| Browser | apa pun yang mendukung Bootstrap 5.3 |

Paket NuGet yang dipakai (semua dipatok, jangan diubah tanpa alasan):

| Proyek | Paket | Versi |
|---|---|---|
| `WebKaryawan.Core` | Microsoft.EntityFrameworkCore.Sqlite | 10.0.12 |
| `WebKaryawan.Core` | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.12 |
| `WebKaryawan.Api` | Microsoft.EntityFrameworkCore.Design | 10.0.12 |
| `WebKaryawan.Api` | Microsoft.AspNetCore.OpenApi | 10.0.13 |
| `WebKaryawan.Tests` | Microsoft.AspNetCore.Mvc.Testing | 10.0.12 |
| `WebKaryawan.Tests` | xunit | 2.9.3 |
| `WebKaryawan.Tests` | xunit.runner.visualstudio | 3.1.4 |
| `WebKaryawan.Tests` | Microsoft.NET.Test.Sdk | 17.14.1 |
| `WebKaryawan.Tests` | coverlet.collector | 6.0.4 |

Bootstrap 5.3.3 dan jQuery 3.7.1 diambil dari CDN jsdelivr, jadi **butuh koneksi internet**
saat halaman dibuka. Asetnya tidak di-vendor ke dalam repo.

---

## Mulai Cepat

```bash
git clone https://github.com/shua24/web_karyawan.git
cd web_karyawan
```

### 1. Buat berkas konfigurasi lokal

Berkas `WebKaryawan.Api/appsettings.Development.json` **sengaja tidak masuk repositori**
(berisi kata sandi admin). Aplikasi akan gagal start tanpa berkas ini. Buat sendiri:

```json
{
  "ConnectionStrings": {
    "Bawaan": "Data Source=webkaryawan.db"
  },
  "AkunAdmin": {
    "NamaPengguna": "admin",
    "KataSandi": "PilihKataSandi123!"
  }
}
```

Syarat kata sandi dari ASP.NET Core Identity: **minimal 8 karakter, ada angka, ada huruf
kecil**. Kalau tidak terpenuhi, aplikasi berhenti saat start dengan pesan yang jelas — itu
pengaman yang bekerja, bukan bug.

### 2. Jalankan

```bash
dotnet run --project WebKaryawan.Api
```

Buka **http://localhost:5080** — otomatis diarahkan ke halaman login.

Masuk dengan `admin` dan kata sandi yang baru saja kamu tulis. Database `webkaryawan.db`
dibuat otomatis saat pertama kali jalan, lengkap dengan 5 data contoh dan 249 negara.

> `dotnet run` tanpa `--project` dari direktori akar akan gagal dengan pesan
> `Couldn't find a project to run`. Solusinya: tambahkan `--project WebKaryawan.Api`, atau
> `cd WebKaryawan.Api` dulu.

---

## Struktur Repositori

```
web_karyawan/
├── WebKaryawan.slnx                  Solusi (format XML baru, bukan .sln)
├── WebKaryawan.Core/                 Domain — tanpa ketergantungan ke Api
│   ├── Entitas/
│   │   ├── Karyawan.cs               Entitas utama, PK = Nik (string)
│   │   └── Negara.cs                 Entitas negara, PK = Id (kode ISO 2 huruf)
│   ├── Data/
│   │   └── AplikasiDbContext.cs      DbContext + Identity + konfigurasi kolom
│   ├── Masukan/
│   │   └── KaryawanMasukan.cs        Bentuk data masuk dari klien
│   └── Validasi/
│       ├── GalatValidasi.cs          Record { Kode, Pesan }
│       └── ValidasiKaryawan.cs       Semua aturan validasi, di server
│
├── WebKaryawan.Api/                  Aplikasi web
│   ├── Program.cs                    Hanya wiring (~79 baris)
│   ├── Seed/SeedingAwal.cs           Seeding negara, akun admin, data contoh
│   ├── Endpoint/
│   │   ├── EndpointAutentikasi.cs    login, daftar, keluar, sesi
│   │   ├── EndpointKaryawan.cs       CRUD karyawan
│   │   └── EndpointNegara.cs         Daftar negara untuk dropdown
│   ├── Data/negara-id.json           249 nama negara Bahasa Indonesia
│   ├── Properties/launchSettings.json  Port 5080 diatur DI SINI
│   ├── WebKaryawan.Api.http          Contoh permintaan HTTP
│   └── wwwroot/                      Front-end statis, disajikan apa adanya
│       ├── index.html                Monitoring
│       ├── login.html  daftar.html
│       ├── tambah.html ubah.html
│       ├── css/aplikasi.css
│       └── js/
│           ├── aplikasi.js           Halaman monitoring
│           └── form-karyawan.js      Dipakai bersama oleh tambah & ubah
│
└── WebKaryawan.Tests/
    ├── PabrikAplikasiUji.cs          WebApplicationFactory + SQLite in-memory
    ├── PabrikAplikasiBerkasUji.cs    Varian berkas SQLite, untuk menguji migrasi
    ├── KredensialUji.cs              Kredensial khusus test
    ├── IntegrasiKaryawanTests.cs     11 test integrasi CRUD & otorisasi
    └── MigrasiDatabaseTests.cs       2 test migrasi dari database kosong
```

**Yang dihasilkan, jangan diedit manual:** `bin/`, `obj/`, dan `webkaryawan.db`.
Ketiganya diabaikan git.

**Tidak ada di repositori:** `appsettings.Development.json` (rahasia, kamu buat sendiri) dan
`webkaryawan.db` (dibuat otomatis saat jalan).

---

## Arsitektur

Arah ketergantungan satu arah:

```
WebKaryawan.Tests  →  WebKaryawan.Api  →  WebKaryawan.Core
```

`Core` tidak tahu apa-apa soal HTTP, jadi aturan bisnis dan validasi bisa dipakai ulang dan
diuji tanpa menjalankan server.

**Tidak ada Razor dan tidak ada Blazor.** `Program.cs` memanggil `UseDefaultFiles()` dan
`UseStaticFiles()`, sehingga `wwwroot/index.html` disajikan sebagai halaman `/`. Semua data
diambil JavaScript lewat `$.ajax` ke endpoint `/api/...`. Konsekuensinya:

- Mengubah HTML/CSS/JS **tidak perlu** rebuild atau restart — cukup refresh browser
  (Ctrl+Shift+R kalau cache menghalangi).
- Mengubah berkas `.cs` **perlu** restart `dotnet run`.

**Autentikasi:** cookie HttpOnly, `SameSite=Lax`, diterbitkan ASP.NET Core Identity. Tidak ada
token di JavaScript, jadi XSS tidak bisa mencuri sesi. Semua endpoint `/api` wajib login;
tulis-menulis tambahan wajib peran `Admin`.

**Otorisasi ditegakkan di server, bukan di UI.** JavaScript memang menyembunyikan tombol
Ubah/Hapus dari peran `User`, tapi itu hanya tampilan. `RequireRole("Admin")` di endpoint
tetap menolak permintaan langsung, dan penolakannya sudah diuji.

**Paging dan pencarian dikerjakan di server.** `Skip`/`Take` dan `LIKE` diterjemahkan EF Core
ke SQL, jadi tidak seluruh tabel ditarik ke memori.

---

## Referensi API

Semua endpoint butuh cookie sesi, kecuali `login` dan `daftar`.

| Metode | Rute | Peran | Keterangan |
|---|---|---|---|
| POST | `/api/auth/login` | — | Body `{ username, password }` |
| POST | `/api/auth/daftar` | — | Body `{ username, password }`, akun baru jadi `User` |
| POST | `/api/auth/keluar` | login | Hapus sesi |
| GET | `/api/auth/sesi` | login | `{ username, peran: [...] }` |
| GET | `/api/negara` | login | `[{ id, nama }]`, urut nama |
| GET | `/api/karyawan` | login | Query: `nik`, `nama`, `halaman`, `ukuranHalaman` |
| GET | `/api/karyawan/{nik}` | login | Detail satu baris |
| POST | `/api/karyawan` | Admin | Tambah |
| PUT | `/api/karyawan/{nik}` | Admin | Ubah — NIK diambil dari rute, bukan body |
| DELETE | `/api/karyawan/{nik}` | Admin | Hapus permanen |

Bentuk balasan `GET /api/karyawan`:

```json
{
  "totalBaris": 5,
  "nomorHalaman": 1,
  "ukuranHalaman": 10,
  "daftar": [
    {
      "nik": "3171010101870001",
      "namaLengkap": "Lionel Messi",
      "jenisKelamin": "Laki-laki",
      "tanggalLahir": "1987-01-01",
      "alamat": "Jl. Achmad Yani No 89 Jakarta Pusat",
      "negara": "Argentina",
      "umur": 39
    }
  ]
}
```

`umur` dihitung di server dari `tanggalLahir`, tidak disimpan. `tanggalLahir` dikirim dalam
ISO (`yyyy-MM-dd`); JavaScript yang mengubahnya ke `dd-MM-yyyy` untuk ditampilkan.

**Balasan galat selalu berbentuk sama** — kode untuk mesin, pesan untuk manusia:

```json
{
  "galat": [
    { "kode": "NIK_TIDAK_VALID", "pesan": "NIK harus 16 digit angka." }
  ]
}
```

JavaScript memetakan `kode` menjadi teks yang ditampilkan, jadi bahasa UI tinggal di satu
tempat. Kode HTTP: `400` validasi gagal, `401` belum login, `403` tidak berhak, `404` tidak
ditemukan.

Contoh cepat dengan curl:

```bash
curl -s -c /tmp/jar.txt -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"KataSandiKamu123!"}'

curl -s -b /tmp/jar.txt http://localhost:5080/api/karyawan
```

---

## Model Data

**Karyawan**

| Kolom | Tipe | Aturan |
|---|---|---|
| `Nik` | TEXT | Primary key, wajib, **persis 16 digit angka** |
| `NamaLengkap` | TEXT | wajib |
| `JenisKelamin` | TEXT | wajib, hanya `Laki-laki` atau `Perempuan` |
| `TanggalLahir` | DateOnly | wajib, tidak boleh masa depan |
| `Alamat` | TEXT | wajib |
| `NegaraId` | TEXT | wajib, foreign key ke `Negara.Id` |

### Kenapa NIK disimpan TEXT padahal spesifikasi bilang "Numeric"

Spesifikasi menyebut NIK bertipe **Numeric**. Proyek ini menyimpannya sebagai **TEXT**, dan itu
keputusan sadar, bukan kelalaian.

Akar masalahnya: kata "numeric" di spesifikasi itu mendeskripsikan **isi** — deretan angka —
bukan **tipe data**. NIK tidak pernah dijumlahkan, dirata-rata, atau diurutkan secara aritmetika.
Tidak ada operasi matematika yang sah atasnya, jadi memperlakukannya sebagai bilangan tidak
memberi keuntungan apa pun dan justru merusak nilainya. Yang dibutuhkan adalah kolom yang
menjamin isinya 16 digit angka, dan itu urusan validasi, bukan tipe kolom.

Dua kerusakan konkret kalau dipaksa INTEGER, keduanya sudah diuji:

**1. NIK berawalan nol hilang kalau disimpan sebagai angka.**

```
0123456789012345  ->  tersimpan jadi 123456789012345  (15 digit)
```

NIK adalah identitas, bukan bilangan yang dihitung. Kalau nol di depan hilang, NIK tersimpan
berbeda dari yang diketik, dan dua NIK yang berbeda (`0123456789012345` dan `123456789012345`)
bisa bertabrakan sebagai primary key. Dengan TEXT, terbukti tersimpan utuh — sudah diuji
end-to-end lewat API dan database.

**2. Angka 16 digit kehilangan presisi di JavaScript.**

`Number.MAX_SAFE_INTEGER` adalah `9007199254740991` (16 digit, tapi batasnya di tengah).
NIK di atas angka itu berubah nilainya begitu melewati `JSON.parse` di browser:

```
9999999999999999  ->  JSON.parse  ->  10000000000000000   (bertambah satu digit)
```

Artinya kalau API mengirim NIK sebagai angka JSON, browser bisa menampilkan dan mengirim balik
NIK yang **berbeda dari yang tersimpan**. Untuk kolom identitas, itu kerusakan data yang tidak
bisa dideteksi pengguna.

**Jadi yang dipenuhi adalah perilakunya, bukan tipe kolomnya.** NIK dijamin numerik di level
validasi, dan itu yang penting bagi pengguna:

| Uji | Hasil |
|---|---|
| ada huruf (`31710101019900AB`) | ditolak `NIK_TIDAK_VALID` |
| tanda minus (`-171010101990009`) | ditolak |
| 17 digit (`31710101019900091`) | ditolak |
| spasi di tengah (`3171 010101990009`) | ditolak |
| 15 digit (`537005696745001`) | ditolak |
| 16 digit angka murni | diterima |

**Untuk klien API:** kirim NIK sebagai **string**, bukan angka:

```json
{ "nik": "3171010101870001" }
```

Bukan:

```json
{ "nik": 3171010101870001 }
```

Bentuk angka akan ditolak oleh server dengan `400`, karena field `Nik` bertipe `string` dan
tidak ada konverter yang menerima angka. Kalau kamu perlu menerima kedua bentuk (misalnya
karena ada klien lain yang mengikuti spesifikasi secara harfiah), tambahkan `JsonConverter`
pada `KaryawanMasukan.Nik` yang membaca `JsonTokenType.Number` sebagai `long` lalu
mengubahnya ke string.

Kalau di kemudian hari tipe ini benar-benar harus jadi INTEGER (misal karena tuntutan
integrasi eksternal), konsekuensinya harus diterima lebih dulu: NIK berawalan nol tidak bisa
dipakai, dan NIK di atas `9007199254740991` tidak aman ditampilkan di web tanpa penanganan
khusus.

**Negara**

| Kolom | Tipe | Aturan |
|---|---|---|
| `Id` | TEXT | Primary key, kode ISO 3166-1 alpha-2 (`ID`, `AR`, `PT`) |
| `Nama` | TEXT | wajib, nama Bahasa Indonesia |

Tabel `Negara` di-seed 249 entri dari daftar ISO 3166 lengkap berbahasa Indonesia. Jumlahnya
249 karena sumber itu memuat wilayah dependen (Anguilla, Antartika, dan sejenisnya), bukan
hanya negara berdaulat. Foreign key memakai `DeleteBehavior.Restrict`, jadi negara yang masih
dipakai tidak bisa dihapus.

**Skema dikelola dengan migrasi EF Core**, dijalankan otomatis saat aplikasi start. Berkas
migrasi ada di `WebKaryawan.Core/Migrations/`. Kalau kamu mengubah entitas, buat migrasi baru
(lihat [Migrasi Database](#migrasi-database)) — jangan hapus database sebagai cara biasa.

---

## Aturan Validasi

Semua aturan ini ada di `WebKaryawan.Core/Validasi/ValidasiKaryawan.cs` dan dijalankan di
server. JavaScript punya salinan sebagian untuk kenyamanan mengisi formulir, tapi **server
tetap sumber kebenaran** — peran `User` bisa menembak API langsung.

| Kode | Kapan muncul |
|---|---|
| `NIK_WAJIB` | NIK kosong |
| `NIK_TIDAK_VALID` | bukan 16 digit angka |
| `NIK_SUDAH_TERDAFTAR` | NIK sudah dipakai (hanya saat tambah) |
| `NAMA_WAJIB` | nama kosong |
| `JENIS_KELAMIN_TIDAK_VALID` | bukan `Laki-laki`/`Perempuan` |
| `TANGGAL_LAHIR_WAJIB` | tanggal kosong |
| `TANGGAL_MASA_DEPAN` | tanggal lahir di masa depan |
| `ALAMAT_WAJIB` | alamat kosong |
| `NEGARA_WAJIB` | negara belum dipilih |
| `NEGARA_TIDAK_DIKENAL` | `NegaraId` tidak ada di tabel |
| `TIDAK_DITEMUKAN` | NIK yang diminta tidak ada |
| `TIDAK_BERHAK` | peran tidak cukup (dipakai di sisi klien) |

Validasi mengembalikan **semua** galat sekaligus, bukan berhenti di yang pertama, supaya
pengguna bisa membetulkan semuanya dalam satu kali kirim.

---

## Menjalankan Test

```bash
dotnet test
```

Hasil terverifikasi:

```
Passed!  - Failed:     0, Passed:    13, Skipped:     0, Total:    13, Duration: 2 s
```

Test ini **integrasi**, bukan unit: setiap test menembak endpoint HTTP sungguhan lewat
`WebApplicationFactory`, dengan SQLite in-memory. Jadi yang diuji adalah perilaku yang
dilihat klien, termasuk status 401/403 dan bentuk JSON galat.

| Test | Yang dijaga |
|---|---|
| `TanpaLogin_Ditolak401` | endpoint terlindungi |
| `SetelahLogin_SeedTerbacaUmurTerhitungDanUrutNik` | seeding, umur, urutan |
| `CariNikDanNama_DigabungAnd` | semantik pencarian |
| `Paging_MembatasiDanMelaporkanTotal` | paging + total |
| `TambahTanpaNik16Digit_MenolakDenganKodeTerstruktur` | format NIK |
| `TambahNikDuplikat_MenolakDenganKodeNikSudahTerdaftar` | NIK unik |
| `AlurLengkap_TambahUbahBacaHapus` | CRUD utuh |
| `UserBiasa_BolehBacaTapiDitolakSaatMenulis` | otorisasi 403 |
| `DaftarAkunBaru_MasukRoleUserBukanAdmin` | registrasi tidak memberi Admin |
| `Keluar_SetelahKeluarDitolak401` | sesi benar-benar berakhir |
| `TanggalLahirMasaDepan_Ditolak` | tanggal wajar |
| `Migrasi_MembangunSkemaDariKosongDanAplikasiBerfungsi` | migrasi membangun 12 tabel + `__EFMigrationsHistory` |
| `AplikasiBerjalanDiAtasDatabaseHasilMigrasi` | app jalan di DB hasil migrasi (5 seed, 249 negara) |

Dua test terakhir sengaja **tidak** memakai SQLite in-memory, karena pabrik in-memory
menyuntikkan koneksinya sendiri sehingga `Database.MigrateAsync()` tidak pernah dijalankan.
Keduanya memakai berkas SQLite sungguhan di direktori sementara.

Test **tidak** butuh `appsettings.Development.json` — kredensialnya disuntikkan sendiri lewat
`KredensialUji.cs`. Ini disengaja: kalau test mewarisi konfigurasi lokal, suite jadi bergantung
pada rahasia mesin pengembang dan kata sandi asli ikut tertulis di berkas yang masuk repositori.

---

## Migrasi Database

Skema dikelola dengan **EF Core Migrations**. Migrasi berjalan **otomatis saat aplikasi
start** (`SeedingAwal` memanggil `Database.MigrateAsync()`), jadi developer baru cukup
`dotnet run` — tidak ada langkah tambahan.

Berkas migrasi ada di `WebKaryawan.Core/Migrations/` dan **ikut masuk repositori**:

```
20261024092504_InitialCreate.cs            migrasi pertama (Up/Down)
20261024092504_InitialCreate.Designer.cs   metadata
AplikasiDbContextModelSnapshot.cs          snapshot model saat ini
```

### Alat `dotnet ef` sudah dipatok di repo

Tidak perlu instalasi global. Alatnya dideklarasikan di `.config/dotnet-tools.json`
(versi 10.0.12), jadi cukup sekali di awal:

```bash
dotnet tool restore
dotnet ef --version
```

Kalau muncul `Could not execute because the specified command or file was not found`,
`dotnet tool restore` belum dijalankan di direktori repo.

### Perintah yang sering dipakai

Semua perintah butuh `--project` dan `--startup-project`, karena DbContext ada di `Core`
sementara konfigurasi dan connection string ada di `Api`:

```bash
# Lihat daftar migrasi dan mana yang sudah diterapkan
dotnet ef migrations list --project WebKaryawan.Core --startup-project WebKaryawan.Api

# Terapkan migrasi secara manual (tanpa menjalankan aplikasi)
dotnet ef database update --project WebKaryawan.Core --startup-project WebKaryawan.Api

# Cek apakah model berubah tapi belum dibuatkan migrasi
dotnet ef migrations has-pending-model-changes --project WebKaryawan.Core --startup-project WebKaryawan.Api

# Buat migrasi baru setelah mengubah entitas
dotnet ef migrations add NamaPerubahan --project WebKaryawan.Core --startup-project WebKaryawan.Api

# Batalkan migrasi terakhir (hanya kalau BELUM diterapkan ke database)
dotnet ef migrations remove --project WebKaryawan.Core --startup-project WebKaryawan.Api
```

`migrations has-pending-model-changes` adalah pemeriksaan termurah sebelum commit: kalau
jawabannya `No changes have been made to the model since the last migration`, berarti skema
dan kode masih sinkron.

### Alur mengubah skema

1. Ubah entitas di `WebKaryawan.Core/Entitas/`.
2. `dotnet ef migrations add NamaPerubahan ...` (perintah di atas).
3. **Baca berkas migrasi yang dihasilkan.** Pastikan `Up()` berisi perubahan yang kamu maksud —
   migrasi yang salah tapi "berhasil jalan" lebih berbahaya daripada yang gagal.
4. Jalankan `dotnet test`. Ada test yang benar-benar menjalankan migrasi ke database kosong.
5. Jalankan aplikasi; migrasi diterapkan otomatis.
6. Commit berkas migrasi bersama perubahan entitas. Jangan pernah commit perubahan entitas
   tanpa migrasinya.

### Kenapa `EnsureCreated()` diganti `Migrate()`

`EnsureCreated()` membuat skema langsung tanpa mencatat apa pun di `__EFMigrationsHistory`.
Database yang dibuatnya **tidak bisa dimigrasikan lagi** — menjalankan
`dotnet ef database update` di atasnya gagal dengan:

```
SQLite Error 1: 'table "AspNetRoles" already exists'.
```

Sudah diuji dan terbukti. Karena itu `SeedingAwal` sekarang memakai `MigrateAsync()`.
**Jangan dikembalikan ke `EnsureCreated()`.** Kalau kamu menemukan database lama yang dibuat
cara itu, hapus saja — isinya hanya data seed:

```bash
rm -f WebKaryawan.Api/webkaryawan.db WebKaryawan.Api/webkaryawan.db-shm WebKaryawan.Api/webkaryawan.db-wal
```

### Pindah ke database lain (PostgreSQL, SQL Server)

Struktur proyek sudah siap untuk ini: semua provider didaftarkan di satu tempat
(`Program.cs`), dan migrasi tinggal di `Core`. Yang perlu dilakukan:

1. Tambah paket provider, misal `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Ganti `UseSqlite(...)` menjadi `UseNpgsql(...)` di `Program.cs` dan
   `PabrikAplikasiBerkasUji.cs`.
3. Ubah `ConnectionStrings:Bawaan` di `appsettings.Development.json`.
4. **Buat ulang migrasi** — SQL yang dihasilkan bergantung provider, jadi migrasi SQLite tidak
   bisa dipakai di PostgreSQL. Hapus folder `Migrations/`, lalu `dotnet ef migrations add InitialCreate`.
5. Perhatikan dua hal yang bersifat khas SQLite: `EF.Functions.Collate(x, "NOCASE")` di
   `EndpointNegara` tidak ada di PostgreSQL (pakai `ILIKE` atau collation ICU), dan tipe kolom
   `"TEXT"` sebaiknya diganti tipe asli provider.

Migrasi untuk SQLite **tidak** otomatis berlaku untuk provider lain. Itu batasan EF Core, bukan
kekurangan proyek ini.

---

## Jebakan yang Wajib Diketahui

Semuanya sudah menggigit saat proyek ini dibangun. Membacanya lebih murah daripada menemukannya.

### Mengubah kata sandi admin setelah database ada tidak berpengaruh

Seeding hanya membuat akun kalau belum ada. Kalau `webkaryawan.db` sudah berisi admin, mengubah
`AkunAdmin:KataSandi` **tidak mengubah apa pun** — login tetap memakai kata sandi lama.
Terverifikasi: setelah config diubah, kata sandi baru ditolak `401` dan yang lama tetap `200`.

Kalau memang mau ganti, hapus database lalu jalankan ulang:

```bash
rm -f WebKaryawan.Api/webkaryawan.db WebKaryawan.Api/webkaryawan.db-shm WebKaryawan.Api/webkaryawan.db-wal
```

Efek sampingnya: 5 data contoh dan semua akun `User` ikut hilang, lalu di-seed ulang.

### `ASPNETCORE_URLS` diabaikan

`dotnet run` memakai `applicationUrl` dari `Properties/launchSettings.json` dan **mengabaikan**
variabel lingkungan `ASPNETCORE_URLS`. Gejalanya membingungkan: kamu set port, aplikasi tetap
listen di port lain. Ubah port di `launchSettings.json`, dan percayai baris
`Now listening on:` — bukan variabel yang kamu set.

### Penolakan peran jadi 404, bukan 403

Dengan cookie auth, `RequireRole` yang gagal mengarahkan (302) ke `/Account/AccessDenied` yang
tidak ada, sehingga klien menerima **404**. Penolakan login juga mengarah ke `/Account/Login`.
Keduanya di-override di `Program.cs` (`OnRedirectToLogin`, `OnRedirectToAccessDenied`) supaya
API menjawab `401`/`403`. **Jangan hapus dua handler itu** — tanpa yang kedua, test otorisasi
akan gagal dengan `Expected: Forbidden, Actual: NotFound`.

### NIK read-only saat ubah mematahkan validasi PUT

Formulir ubah mengunci NIK (nilainya hanya ada di rute), jadi body PUT tidak membawa NIK. Kalau
validator mewajibkan NIK dari body, PUT gagal `400`. Karena itu `ValidasiAsync` menerima
`nikDariRute` sebagai pengganti. Kalau kamu menambah aturan validasi NIK, hormati parameter itu.

### SQLite mengurutkan dengan collation BINARY

`OrderBy(x => x.Nama)` menaruh nama berhuruf kecil di belakang: "eSwatini" muncul setelah
"Zimbabwe". Perbaikannya `EF.Functions.Collate(x.Nama, "NOCASE")`. Ini sudah diterapkan di
`EndpointNegara`; ikuti pola yang sama untuk kolom teks lain yang diurutkan.

### Umur harus bisa diterjemahkan ke SQL

`umur` dihitung di dalam `Select` supaya EF Core menerjemahkannya ke SQL. Memanggil method C#
biasa di dalam `Select` akan melempar galat saat runtime karena tidak bisa diterjemahkan.
Rumusnya sengaja diduplikasi di dua query (daftar dan detail) — kalau mengubahnya, ubah keduanya.

### SQLite in-memory mati di antara request

Satu `SqliteConnection` harus dibuka di `ConfigureWebHost` dan baru ditutup di `Dispose`.
Kalau ditutup lebih awal, seluruh tabel hilang di antara request dan test gagal secara aneh.
Lihat `PabrikAplikasiUji.cs`.

### `.gitignore` peka huruf besar/kecil

`bin/` saja **tidak** menangkap `Bin/`, `BIN/`, atau `bIn/`. Pola di repo ini memakai
`[Bb][Ii][Nn]/` dan `[Oo][Bb][Jj]/` supaya semua variasi tertangkap. Ini penting karena
`appsettings.Development.json` ikut ter-copy ke `bin/` saat build — kalau `bin/` lolos, rahasia
ikut masuk git. Sudah diuji: 656 berkas di dalam `bin/`/`obj/`, nol yang lolos.

### `pkill -f "WebKaryawan"` bisa membunuh shell-mu sendiri

Pola itu cocok dengan baris perintah shell yang menjalankannya. Ambil PID dari soket:

```bash
PID=$(ss -ltnp 2>/dev/null | grep ':5080' | grep -oP 'pid=\K[0-9]+' | head -1)
kill "$PID"
```

---

## Cara Memperluas

### Menambah kolom pada Karyawan

1. Tambah properti di `WebKaryawan.Core/Entitas/Karyawan.cs`.
2. Daftarkan di `OnModelCreating` (`AplikasiDbContext.cs`) kalau butuh tipe kolom khusus.
3. Tambah properti di `KaryawanMasukan.cs`.
4. Tambah aturan di `ValidasiKaryawan.cs` dengan kode galat baru.
5. Petakan kode baru di `petaField`/`pesanGalat` di `js/aplikasi.js` dan `js/form-karyawan.js`.
6. Tambah input di `tambah.html` dan `ubah.html`, tambah kolom di `index.html` (termasuk
   `colspan` pada baris kosong — sekarang `9`).
7. Tambah ke `Select` di `EndpointKaryawan.cs` (daftar dan detail).
8. Buat migrasi: `dotnet ef migrations add TambahKolomBaru --project WebKaryawan.Core --startup-project WebKaryawan.Api`.
9. Jalankan `dotnet test`, lalu jalankan aplikasi (migrasi diterapkan otomatis), dan tambah test.

### Mengubah tampilan

Semua warna ada di variabel CSS di bagian atas `wwwroot/css/aplikasi.css`:

```css
--biru-tua      header tabel
--biru-tombol   tombol utama
--peach-panel   panel pencarian
--merah-aksi    tautan Detail/Ubah/Hapus
--garis-tabel   garis tabel
```

Ubah di situ, seluruh halaman ikut. Tidak perlu rebuild.

### Konvensi kode

- PascalCase untuk tipe dan anggota; camelCase untuk variabel lokal dan parameter.
- **Nama ditulis penuh, tanpa singkatan** — `KaryawanMasukan`, bukan `KaryawanDto`;
  `nomorHalaman`, bukan `hal`.
- Setiap nilai balik method ditampung ke variabel lokal bernama sebelum dipakai.
- Berkas maksimal **300 baris**, berlaku untuk kode yang ditulis tangan. Yang terbesar sekarang
  294 baris (`IntegrasiKaryawanTests.cs`). Berkas JavaScript/HTML/CSS sedikit melewati batas
  (`js/aplikasi.js` 332 baris) karena memuat tampilan, bukan logika domain.
  **Pengecualian: `WebKaryawan.Core/Migrations/`** — berkas di situ dihasilkan `dotnet ef`
  (`.Designer.cs` 324 baris, `AplikasiDbContextModelSnapshot.cs` 321 baris) dan tidak boleh
  diedit atau dipotong manual.
  `Program.cs` sengaja hanya berisi wiring; endpoint tinggal di `Endpoint/`.
- Komentar dan pesan dalam Bahasa Indonesia.

---

## Yang Belum Ada

Diketahui dan disengaja, bukan kelupaan:

- **Migrasi baru hanya untuk SQLite.** Skema dikelola EF Core Migrations dan diterapkan
  otomatis saat start. Kalau nanti pindah provider, migrasi harus dibuat ulang — lihat
  [Migrasi Database](#migrasi-database).
- **Cookie belum `Secure`.** Berjalan di HTTP, aman untuk localhost, **tidak** untuk jaringan.
  Untuk deploy, aktifkan HTTPS dan set `Cookie.SecurePolicy`.
- **Kata sandi admin berupa plaintext** di `appsettings.Development.json`. Dapat diterima untuk
  demo lokal; untuk produksi pakai environment variable atau secret manager.
- **Registrasi terbuka.** Akun baru hanya bisa melihat, tapi siapa pun yang membuka
  `/daftar.html` tetap bisa membuat akun dan **membaca seluruh data pribadi**. Kalau aplikasi ini
  dipakai sungguhan, pendaftaran perlu persetujuan admin.
- **Hapus bersifat permanen** (hard delete), tanpa arsip dan tanpa jejak audit.
- **Umur mengikuti zona waktu server**, bukan WIB secara eksplisit. Di-deploy ke UTC, umur bisa
  berbeda satu hari di sekitar tengah malam.
- **Belum ada dwibahasa.** UI Indonesia saja; pesan galat sudah dipisah per kode, jadi
  penambahan bahasa nanti cukup di lapisan tampilan.

---

## Lisensi

Repositori ini **belum memiliki berkas `LICENSE`**. Sebelum dipakai ulang atau didistribusikan,
pemilik proyek perlu menetapkan lisensinya lebih dulu.
