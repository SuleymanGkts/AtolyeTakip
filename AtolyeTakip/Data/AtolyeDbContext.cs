using Microsoft.EntityFrameworkCore;
using AtolyeTakip.Models;

namespace AtolyeTakip.Data
{
    public class AtolyeDbContext : DbContext
    {
        public AtolyeDbContext(DbContextOptions<AtolyeDbContext> options) : base(options) { }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<UretimKaydi> UretimKayitlari { get; set; }
        public DbSet<Parca> Parcalar { get; set; }
        public DbSet<Tezgah> Tezgahlar { get; set; } // Tezgah tablosu

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Başlangıç Kullanıcıları (Şifre: 1234)
            modelBuilder.Entity<Kullanici>().HasData(
                new Kullanici { Id = 1, AdSoyad = "Fabrika Mühendisi", KullaniciAdi = "admin", Rol = "Muhendis", SifreHash = BCrypt.Net.BCrypt.HashPassword("1234") },
                new Kullanici { Id = 2, AdSoyad = "Ahmet Personel", KullaniciAdi = "ahmet", Rol = "Personel", SifreHash = BCrypt.Net.BCrypt.HashPassword("1234") },
                new Kullanici { Id = 3, AdSoyad = "Mehmet Personel", KullaniciAdi = "mehmet", Rol = "Personel", SifreHash = BCrypt.Net.BCrypt.HashPassword("1234") }
            );

            // 3 Vardiyalı Başlangıç Parçaları
            modelBuilder.Entity<Parca>().HasData(
                new Parca
                {
                    Id = 1,
                    ParcaAdi = "Gövde Flanşı - A1",
                    HedefSureDk = 15,
                    HedefMiktarVardiya1 = 35,
                    HedefMiktarVardiya2 = 28,
                    HedefMiktarVardiya3 = 20
                },
                new Parca
                {
                    Id = 2,
                    ParcaAdi = "Mil Yatağı - B2",
                    HedefSureDk = 25,
                    HedefMiktarVardiya1 = 20,
                    HedefMiktarVardiya2 = 16,
                    HedefMiktarVardiya3 = 12
                }
            );
        }
    }
}