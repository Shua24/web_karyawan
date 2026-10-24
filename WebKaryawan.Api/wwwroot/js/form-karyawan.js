(function () {
    'use strict';

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

    var keadaan = {
        mode: 'tambah',
        nik: null
    };

    function lolosHtml(teks) {
        return $('<div>').text(teks === null || teks === undefined ? '' : teks).html();
    }

    function pesanUntuk(kodeGalat, pesanDariServer) {
        return pesanGalat[kodeGalat] || pesanDariServer || 'Terjadi kesalahan.';
    }

    function ambilNikDariAlamat() {
        var parameter = new URLSearchParams(window.location.search);
        return parameter.get('nik');
    }

    function isiPilihanNegara(daftar, terpilih) {
        var pilihan = $('#pilihNegara');
        pilihan.empty();
        pilihan.append('<option value="">Pilih Negara</option>');

        daftar.forEach(function (negara) {
            var opsi = $('<option></option>')
                .attr('value', negara.id)
                .text(negara.nama);

            if (negara.id === terpilih) {
                opsi.prop('selected', true);
            }

            pilihan.append(opsi);
        });
    }

    function kosongkanGalat() {
        $('.pesan-galat').text('');
        $('#ringkasanGalat').removeClass('tampil').empty();
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

    function kumpulkanMasukan() {
        var masukan = {
            namaLengkap: $('#inputNamaLengkap').val().trim(),
            jenisKelamin: $('input[name="jenisKelamin"]:checked').val() || '',
            tanggalLahir: $('#inputTanggalLahir').val() || null,
            alamat: $('#inputAlamat').val().trim(),
            negaraId: $('#pilihNegara').val() || ''
        };

        if (keadaan.mode === 'tambah') {
            masukan.nik = $('#inputNik').val().trim();
        }

        return masukan;
    }

    function validasiSisiKlien(masukan) {
        var daftarGalat = [];

        if (keadaan.mode === 'tambah') {
            if (!masukan.nik) {
                daftarGalat.push({ kode: 'NIK_WAJIB' });
            } else if (!/^[0-9]{16}$/.test(masukan.nik)) {
                daftarGalat.push({ kode: 'NIK_TIDAK_VALID' });
            }
        }

        if (!masukan.namaLengkap) {
            daftarGalat.push({ kode: 'NAMA_WAJIB' });
        }

        if (!masukan.jenisKelamin) {
            daftarGalat.push({ kode: 'JENIS_KELAMIN_TIDAK_VALID' });
        }

        if (!masukan.tanggalLahir) {
            daftarGalat.push({ kode: 'TANGGAL_LAHIR_WAJIB' });
        }

        if (!masukan.alamat) {
            daftarGalat.push({ kode: 'ALAMAT_WAJIB' });
        }

        if (!masukan.negaraId) {
            daftarGalat.push({ kode: 'NEGARA_WAJIB' });
        }

        return daftarGalat;
    }

    function kirim(masukan) {
        var alamat = '/api/karyawan';
        var metode = 'POST';

        if (keadaan.mode === 'ubah') {
            alamat = '/api/karyawan/' + encodeURIComponent(keadaan.nik);
            metode = 'PUT';
        }

        return $.ajax({
            url: alamat,
            method: metode,
            contentType: 'application/json',
            dataType: 'json',
            data: JSON.stringify(masukan)
        });
    }

    function muatDataAwal() {
        var permintaanNegara = $.ajax({ url: '/api/negara', method: 'GET', dataType: 'json' });

        if (keadaan.mode !== 'ubah') {
            return permintaanNegara.then(function (daftar) {
                isiPilihanNegara(daftar, null);
            });
        }

        var permintaanDetail = $.ajax({
            url: '/api/karyawan/' + encodeURIComponent(keadaan.nik),
            method: 'GET',
            dataType: 'json'
        });

        return $.when(permintaanNegara, permintaanDetail).then(function (hasilNegara, hasilDetail) {
            var daftarNegara = hasilNegara[0];
            var detail = hasilDetail[0];

            isiPilihanNegara(daftarNegara, detail.negaraId);
            $('#inputNik').val(detail.nik);
            $('#inputNamaLengkap').val(detail.namaLengkap);
            $('#inputTanggalLahir').val(String(detail.tanggalLahir).substring(0, 10));
            $('#inputAlamat').val(detail.alamat);

            $('input[name="jenisKelamin"]').each(function () {
                var radio = $(this);
                radio.prop('checked', radio.val() === detail.jenisKelamin);
            });
        });
    }

    function pasangPeristiwa() {
        $('#formKaryawan').on('submit', function (peristiwa) {
            peristiwa.preventDefault();
            kosongkanGalat();

            var masukan = kumpulkanMasukan();
            var galatKlien = validasiSisiKlien(masukan);

            if (galatKlien.length) {
                tampilkanGalat(galatKlien);
                return;
            }

            kirim(masukan).then(function () {
                window.location.href = '/index.html';
            }, function (xhr) {
                if (xhr.status === 401) {
                    window.location.href = '/login.html';
                    return;
                }

                if (xhr.status === 403) {
                    tampilkanGalat([{ kode: 'TIDAK_BERHAK' }]);
                    return;
                }

                if (xhr.status === 404) {
                    tampilkanGalat([{ kode: 'TIDAK_DITEMUKAN' }]);
                    return;
                }

                var isi = xhr.responseJSON || {};
                tampilkanGalat(isi.galat || []);
            });
        });
    }

    $(function () {
        var nikDariAlamat = ambilNikDariAlamat();

        if (nikDariAlamat) {
            keadaan.mode = 'ubah';
            keadaan.nik = nikDariAlamat;
        }

        if (keadaan.mode === 'tambah' && !$('#inputNik').length) {
            window.location.href = '/index.html';
            return;
        }

        muatDataAwal().then(pasangPeristiwa).catch(function (xhr) {
            if (xhr && xhr.status === 401) {
                window.location.href = '/login.html';
            }
        });
    });
})();
