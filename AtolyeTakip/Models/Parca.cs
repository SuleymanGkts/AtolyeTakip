namespace AtolyeTakip.Models
{
    public class Parca
    {
        public int Id { get; set; }
        public string ParcaAdi { get; set; } = string.Empty;
        public int HedefSureDk { get; set; } // Parça işleme çevrim süresi (dk)

        // Vardiyalara özel net hedef adetleri
        public int HedefMiktarVardiya1 { get; set; } // 1. Vardiya: 08:00 - 17:45
        public int HedefMiktarVardiya2 { get; set; } // 2. Vardiya: 18:00 - 01:45
        public int HedefMiktarVardiya3 { get; set; } // 3. Vardiya: 02:00 - 07:45
    }
}