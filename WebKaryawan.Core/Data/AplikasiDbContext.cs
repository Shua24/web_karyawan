using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebKaryawan.Core.Entitas;

namespace WebKaryawan.Core.Data;

public class PenggunaAplikasi : IdentityUser
{
}

public class AplikasiDbContext : IdentityDbContext<PenggunaAplikasi>
{
    public AplikasiDbContext(DbContextOptions<AplikasiDbContext> options) : base(options)
    {
    }

    public DbSet<Karyawan> Karyawan => Set<Karyawan>();
    public DbSet<Negara> Negara => Set<Negara>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Negara>(entitas =>
        {
            entitas.HasKey(negara => negara.Id);
            entitas.Property(negara => negara.Id).HasColumnType("TEXT");
            entitas.Property(negara => negara.Nama).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<Karyawan>(entitas =>
        {
            entitas.HasKey(karyawan => karyawan.Nik);
            entitas.Property(karyawan => karyawan.Nik).HasColumnType("TEXT");
            entitas.Property(karyawan => karyawan.NamaLengkap).HasColumnType("TEXT").IsRequired();
            entitas.Property(karyawan => karyawan.JenisKelamin).HasColumnType("TEXT").IsRequired();
            entitas.Property(karyawan => karyawan.TanggalLahir).IsRequired();
            entitas.Property(karyawan => karyawan.Alamat).HasColumnType("TEXT").IsRequired();
            entitas.Property(karyawan => karyawan.NegaraId).HasColumnType("TEXT").IsRequired();

            entitas.HasOne(karyawan => karyawan.Negara)
                .WithMany()
                .HasForeignKey(karyawan => karyawan.NegaraId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
