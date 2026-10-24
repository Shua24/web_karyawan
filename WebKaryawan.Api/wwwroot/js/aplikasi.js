(function () {
    'use strict';

    var UKURAN_HALAMAN = 10;

    var keadaan = {
        nik: '',
        nama: '',
        halaman: 1,
        totalBaris: 0,
        daftarNegara: [],
        peranPengguna: [],
        nikAkanDihapus: null
    };

    var pesanGalat = {
        NIK_WAJIB: 'NIK wajib diisi.',
        NIK_TIDAK_VALID: 'NIK harus 16 digit angka.',
        NIK_SUDAH_TERDAFTAR: 'NIK sudah terdaftar.',
        NAMA_WAJIB: 'Nama lengkap wajib diisi.',
        JENIS_KELAMIN_TIDAK_VALID: 'Jenis kelamin wajib dipilih.',
        TANGGAL_LAHIR_WAJIB: 'Tanggal lahir wajib diisi.',
        TANGGAL_MASA_DEPAN: 'Tanggal lahir tidak boleh di masa depan.',
        ALAMAT_WAJIB: 'Alamat wajib diisi.',
        NEGARA_WAJIB: 'Negara wajib dipilih.',
        NEGARA_TIDAK_DIKENAL: 'Negara tidak dikenal.',
        TIDAK_DITEMUKAN: 'Data tidak ditemukan.',
        TIDAK_BERHAK: 'Anda tidak berhak melakukan tindakan ini.'
    };

    var petaField = {
        NIK_WAJIB: 'nik',
        NIK_TIDAK_VALID: 'nik',
        NIK_SUDAH_TERDAFTAR: 'nik',
        NAMA_WAJIB: 'namaLengkap',
        JENIS_KELAMIN_TIDAK_VALID: 'jenisKelamin',
        TANGGAL_LAHIR_WAJIB: 'tanggalLahir',
        TANGGAL_MASA_DEPAN: 'tanggalLahir',
        ALAMAT_WAJIB: 'alamat',
        NEGARA_WAJIB: 'negaraId',
        NEGARA_TIDAK_DIKENAL: 'negaraId'
    };

    function pesanUntuk(kodeGalat, pesanDariServer) {
        return pesanGalat[kodeGalat] || pesanDariServer || 'Terjadi kesalahan.';
    }

    function formatTanggal(teksIso) {
        if (!teksIso) {
            return '-';
        }
        var bagian = String(teksIso).substring(0, 10).split('-');
        if (bagian.length !== 3) {
            return teksIso;
        }
        return bagian[2] + '-' + bagian[1] + '-' + bagian[0];
    }

    function lolosHtml(teks) {
        return $('<div>').text(teks === null || teks === undefined ? '' : teks).html();
    }

    function penggunaAdalahAdmin() {
        return keadaan.peranPengguna.indexOf('Admin') !== -1;
    }

    function tanganiTidakBerhak() {
        window.location.href = '/login.html';
    }

    function mintaJson(opsiPermintaan) {
        return $.ajax(opsiPermintaan).then(null, function (xhr) {
            if (xhr.status === 401) {
                tanganiTidakBerhak();
            }
            return $.Deferred().reject(xhr).promise();
        });
    }

    function muatSesi() {
        return mintaJson({
            url: '/api/auth/sesi',
            method: 'GET',
            dataType: 'json'
        }).then(function (sesi) {
            keadaan.peranPengguna = sesi.peran || [];
            $('#namaPengguna').text(sesi.username);
        });
    }

    function muatNegara() {
        return mintaJson({
            url: '/api/negara',
            method: 'GET',
            dataType: 'json'
        }).then(function (daftar) {
            keadaan.daftarNegara = daftar;
        });
    }

    function gambarBaris(daftar) {
        var badan = $('#badanTabel');
        badan.empty();

        if (!daftar.length) {
            badan.append(
                '<tr class="baris-kosong"><td colspan="9">Tidak ada data yang cocok.</td></tr>'
            );
            return;
        }

        var nomorAwal = (keadaan.halaman - 1) * UKURAN_HALAMAN;

        daftar.forEach(function (baris, indeks) {
            var tombolEdit = penggunaAdalahAdmin()
                ? '<button type="button" class="aksi-edit" data-nik="' + lolosHtml(baris.nik) + '">Ubah</button>'
                : '';
            var tombolHapus = penggunaAdalahAdmin()
                ? '<button type="button" class="aksi-hapus" data-nik="' + lolosHtml(baris.nik) + '" data-nama="' + lolosHtml(baris.namaLengkap) + '">Hapus</button>'
                : '';

            var barisHtml =
                '<tr>' +
                '<td>' + (nomorAwal + indeks + 1) + '</td>' +
                '<td>' + lolosHtml(baris.nik) + '</td>' +
                '<td>' + lolosHtml(baris.namaLengkap) + '</td>' +
                '<td>' + baris.umur + '</td>' +
                '<td>' + formatTanggal(baris.tanggalLahir) + '</td>' +
                '<td>' + lolosHtml(baris.jenisKelamin) + '</td>' +
                '<td>' + lolosHtml(baris.alamat) + '</td>' +
                '<td>' + lolosHtml(baris.negara) + '</td>' +
                '<td class="text-nowrap">' +
                '<button type="button" class="aksi-detail" data-nik="' + lolosHtml(baris.nik) + '">Detail</button>' +
                tombolEdit +
                tombolHapus +
                '</td>' +
                '</tr>';

            badan.append(barisHtml);
        });
    }

    function gambarNavigasi() {
        var jumlahHalaman = Math.max(1, Math.ceil(keadaan.totalBaris / UKURAN_HALAMAN));
        var navigasi = $('#navigasiHalaman');
        navigasi.empty();

        if (jumlahHalaman <= 1) {
            return;
        }

        for (var nomor = 1; nomor <= jumlahHalaman; nomor++) {
            var kelasAktif = nomor === keadaan.halaman ? ' active' : '';
            navigasi.append(
                '<li class="page-item' + kelasAktif + '">' +
                '<a class="page-link" href="#" data-halaman="' + nomor + '">' + nomor + '</a>' +
                '</li>'
            );
        }
    }

    function gambarInfo() {
        if (keadaan.totalBaris === 0) {
            $('#infoBaris').text('Menampilkan 0 dari 0 data');
            return;
        }

        var nomorAwal = (keadaan.halaman - 1) * UKURAN_HALAMAN + 1;
        var nomorAkhir = Math.min(keadaan.halaman * UKURAN_HALAMAN, keadaan.totalBaris);
        $('#infoBaris').text('Menampilkan ' + nomorAwal + '-' + nomorAkhir + ' dari ' + keadaan.totalBaris + ' data');
    }

    function muatDaftar() {
        return mintaJson({
            url: '/api/karyawan',
            method: 'GET',
            dataType: 'json',
            data: {
                nik: keadaan.nik,
                nama: keadaan.nama,
                halaman: keadaan.halaman,
                ukuranHalaman: UKURAN_HALAMAN
            }
        }).then(function (hasil) {
            keadaan.totalBaris = hasil.totalBaris;
            gambarBaris(hasil.daftar);
            gambarNavigasi();
            gambarInfo();
        });
    }

    function kosongkanGalat() {
        $('.pesan-galat').text('');
        $('#ringkasanGalat').removeClass('tampil').text('');
    }

    function tampilkanGalat(daftarGalat) {
        kosongkanGalat();

        if (!daftarGalat || !daftarGalat.length) {
            $('#ringkasanGalat').addClass('tampil').text('Data tidak dapat disimpan.');
            return;
        }

        var ringkasan = $('<ul class="mb-0"></ul>');
        daftarGalat.forEach(function (galat) {
            var pesan = pesanUntuk(galat.kode, galat.pesan);
            ringkasan.append('<li>' + lolosHtml(pesan) + '</li>');

            var namaField = petaField[galat.kode];
            if (namaField) {
                var tempatGalat = $('.pesan-galat[data-untuk="' + namaField + '"]');
                if (tempatGalat.text()) {
                    tempatGalat.append('<br />');
                }
                tempatGalat.append(lolosHtml(pesan));
            }
        });

        $('#ringkasanGalat').addClass('tampil').empty().append(ringkasan);
    }

    function bukaModalHapus(nik, nama) {
        keadaan.nikAkanDihapus = nik;
        $('#teksKonfirmasiHapus').text('Anda yakin menghapus data ' + nama + ' ?');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalHapus')).show();
    }

    function tampilkanDetail(detail) {
        var isi =
            '<dl class="row baris-detail mb-0">' +
            '<dt class="col-sm-4">NIK</dt><dd class="col-sm-8">' + lolosHtml(detail.nik) + '</dd>' +
            '<dt class="col-sm-4">Nama Lengkap</dt><dd class="col-sm-8">' + lolosHtml(detail.namaLengkap) + '</dd>' +
            '<dt class="col-sm-4">Jenis Kelamin</dt><dd class="col-sm-8">' + lolosHtml(detail.jenisKelamin) + '</dd>' +
            '<dt class="col-sm-4">Tanggal Lahir</dt><dd class="col-sm-8">' + formatTanggal(detail.tanggalLahir) + '</dd>' +
            '<dt class="col-sm-4">Umur</dt><dd class="col-sm-8">' + detail.umur + ' tahun</dd>' +
            '<dt class="col-sm-4">Alamat</dt><dd class="col-sm-8">' + lolosHtml(detail.alamat) + '</dd>' +
            '<dt class="col-sm-4">Negara</dt><dd class="col-sm-8">' + lolosHtml(detail.negara) + '</dd>' +
            '</dl>';

        $('#isiDetail').html(isi);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalDetail')).show();
    }

    function pasangPeristiwa() {
        $('#tombolCari').on('click', function () {
            keadaan.nik = $('#inputCariNik').val().trim();
            keadaan.nama = $('#inputCariNama').val().trim();
            keadaan.halaman = 1;
            muatDaftar();
        });

        $('#tombolReset').on('click', function () {
            $('#inputCariNik').val('');
            $('#inputCariNama').val('');
            keadaan.nik = '';
            keadaan.nama = '';
            keadaan.halaman = 1;
            muatDaftar();
        });

        $('#inputCariNik, #inputCariNama').on('keydown', function (peristiwa) {
            if (peristiwa.key === 'Enter') {
                $('#tombolCari').trigger('click');
            }
        });

        $('#tombolTambah').on('click', function () {
            window.location.href = '/tambah.html';
        });

        $('#navigasiHalaman').on('click', 'a.page-link', function (peristiwa) {
            peristiwa.preventDefault();
            keadaan.halaman = parseInt($(this).data('halaman'), 10);
            muatDaftar();
        });

        $('#badanTabel').on('click', '.aksi-detail', function () {
            var nik = $(this).data('nik');
            mintaJson({ url: '/api/karyawan/' + encodeURIComponent(nik), method: 'GET', dataType: 'json' })
                .then(tampilkanDetail);
        });

        $('#badanTabel').on('click', '.aksi-edit', function () {
            var nik = $(this).data('nik');
            window.location.href = '/ubah.html?nik=' + encodeURIComponent(nik);
        });

        $('#badanTabel').on('click', '.aksi-hapus', function () {
            bukaModalHapus($(this).data('nik'), $(this).data('nama'));
        });

        $('#tombolOke').on('click', function () {
            var nik = keadaan.nikAkanDihapus;
            if (!nik) {
                return;
            }

            mintaJson({
                url: '/api/karyawan/' + encodeURIComponent(nik),
                method: 'DELETE',
                dataType: 'json'
            }).then(function () {
                bootstrap.Modal.getOrCreateInstance(document.getElementById('modalHapus')).hide();
                keadaan.nikAkanDihapus = null;
                muatDaftar();
            }, function (xhr) {
                bootstrap.Modal.getOrCreateInstance(document.getElementById('modalHapus')).hide();
                if (xhr.status === 403) {
                    window.alert(pesanGalat.TIDAK_BERHAK);
                }
            });
        });

        $('#tombolKeluar').on('click', function () {
            mintaJson({ url: '/api/auth/keluar', method: 'POST', dataType: 'json' })
                .always(function () {
                    window.location.href = '/login.html';
                });
        });
    }

    $(function () {
        muatSesi()
            .then(muatNegara)
            .then(muatDaftar)
            .then(pasangPeristiwa)
            .catch(function () {
                $('#badanTabel').html('<tr class="baris-kosong"><td colspan="9">Gagal memuat data.</td></tr>');
            });
    });
})();
