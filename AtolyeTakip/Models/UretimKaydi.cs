using System;

namespace AtolyeTakip.Models
{
    public class UretimKaydi
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public Kullanici? Kullanici { get; set; }
        public string TezgahAdi { get; set; } = string.Empty;

        public string ParcaAdi { get; set; } = string.Empty;
        public int? HedefSureDk { get; set; }
        public int? HedefMiktar { get; set; }

        // 3 Ayrı Miktar Kutusu
        public int SaglamMiktar { get; set; }
        public int HurdaMiktar { get; set; }
        public int ReworkMiktar { get; set; }

        // Toplam Miktar (Sağlam + Hurda + Rework)
        public int ToplamMiktar => SaglamMiktar + HurdaMiktar + ReworkMiktar;

        public string Vardiya { get; set; } = string.Empty;
        public TimeSpan? CalismaBaslangic { get; set; }
        public TimeSpan? CalismaBitis { get; set; }
        public string? Aciklama { get; set; }

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
    }
}