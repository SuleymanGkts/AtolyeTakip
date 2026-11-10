namespace AtolyeTakip.Models
{
    public class Kullanici
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;
        public string AdSoyad { get; set; } = string.Empty; // Personelin / Mühendisin Adı Soyadı
        public string Rol { get; set; } = "Personel"; // "Personel" veya "Muhendis"
    }
}