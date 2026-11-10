using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using AtolyeTakip.Data;
using AtolyeTakip.Models;

namespace AtolyeTakip.Controllers
{
    [Authorize(Roles = "Muhendis,Mühendis")]
    public class MuhendisController : Controller
    {
        private readonly AtolyeDbContext _db;

        public MuhendisController(AtolyeDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. ÜRETİM KAYITLARI & GELİŞMİŞ FİLTRELEME
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? baslangic,
            DateTime? bitis,
            string? tezgah,
            int? personelId,
            string? vardiya)
        {
            var query = _db.UretimKayitlari.Include(u => u.Kullanici).AsQueryable();

            if (baslangic.HasValue)
            {
                query = query.Where(u => u.KayitTarihi >= baslangic.Value.Date);
            }

            if (bitis.HasValue)
            {
                var bitisGunu = bitis.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(u => u.KayitTarihi <= bitisGunu);
            }

            if (!string.IsNullOrWhiteSpace(tezgah))
            {
                query = query.Where(u => u.TezgahAdi == tezgah);
            }

            if (personelId.HasValue && personelId.Value > 0)
            {
                query = query.Where(u => u.KullaniciId == personelId.Value);
            }

            if (!string.IsNullOrWhiteSpace(vardiya))
            {
                query = query.Where(u => u.Vardiya.Contains(vardiya));
            }

            var kayitlar = await query.OrderByDescending(u => u.KayitTarihi).ToListAsync();

            ViewBag.Tezgahlar = await _db.Tezgahlar.OrderBy(t => t.TezgahAdi).ToListAsync();
            ViewBag.Personeller = await _db.Kullanicilar.Where(k => k.Rol != "Muhendis").OrderBy(k => k.AdSoyad).ToListAsync();

            ViewBag.Baslangic = baslangic?.ToString("yyyy-MM-dd");
            ViewBag.Bitis = bitis?.ToString("yyyy-MM-dd");
            ViewBag.SecilenTezgah = tezgah;
            ViewBag.SecilenPersonel = personelId;
            ViewBag.SecilenVardiya = vardiya;

            return View(kayitlar);
        }

        // ==========================================
        // 2. ÜRETİM KAYDI DÜZENLEME & SİLME (YENİ)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KayitGuncelle(
            int id,
            string tezgahAdi,
            int saglamMiktar,
            int hurdaMiktar,
            int reworkMiktar,
            TimeSpan? calismaBaslangic,
            TimeSpan? calismaBitis,
            string? aciklama)
        {
            var kayit = await _db.UretimKayitlari.FindAsync(id);
            if (kayit != null)
            {
                kayit.TezgahAdi = tezgahAdi?.Trim() ?? "";
                kayit.SaglamMiktar = Math.Max(0, saglamMiktar);
                kayit.HurdaMiktar = Math.Max(0, hurdaMiktar);
                kayit.ReworkMiktar = Math.Max(0, reworkMiktar);
                kayit.CalismaBaslangic = calismaBaslangic;
                kayit.CalismaBitis = calismaBitis;
                kayit.Aciklama = aciklama?.Trim();

                await _db.SaveChangesAsync();
                TempData["Mesaj"] = "Üretim kaydı başarıyla güncellendi.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KayitSil(int id)
        {
            var kayit = await _db.UretimKayitlari.FindAsync(id);
            if (kayit != null)
            {
                _db.UretimKayitlari.Remove(kayit);
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = "Üretim kaydı başarıyla silindi.";
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. FİLTRELENMİŞ EXCEL RAPORU (.XLSX)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExcelIndir(
            DateTime? baslangic,
            DateTime? bitis,
            string? tezgah,
            int? personelId,
            string? vardiya)
        {
            var query = _db.UretimKayitlari.Include(u => u.Kullanici).AsQueryable();

            if (baslangic.HasValue)
                query = query.Where(u => u.KayitTarihi >= baslangic.Value.Date);

            if (bitis.HasValue)
            {
                var bitisGunu = bitis.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(u => u.KayitTarihi <= bitisGunu);
            }

            if (!string.IsNullOrWhiteSpace(tezgah))
                query = query.Where(u => u.TezgahAdi == tezgah);

            if (personelId.HasValue && personelId.Value > 0)
                query = query.Where(u => u.KullaniciId == personelId.Value);

            if (!string.IsNullOrWhiteSpace(vardiya))
                query = query.Where(u => u.Vardiya.Contains(vardiya));

            var kayitlar = await query.OrderByDescending(u => u.KayitTarihi).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Vardiya_Uretim_Raporu");

            string[] basliklar = { "Tarih / Saat", "Personel", "Tezgah", "Vardiya", "Parça / İş", "Hedef Mik.", "Toplam", "Sağlam", "Hurda", "Rework", "Çalışma Saatleri", "Açıklama" };
            for (int i = 0; i < basliklar.Length; i++)
            {
                var hucre = ws.Cell(1, i + 1);
                hucre.Value = basliklar[i];
                hucre.Style.Font.Bold = true;
                hucre.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
                hucre.Style.Font.FontColor = XLColor.White;
                hucre.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int satir = 2;
            foreach (var k in kayitlar)
            {
                int toplam = k.SaglamMiktar + k.HurdaMiktar + k.ReworkMiktar;

                ws.Cell(satir, 1).Value = k.KayitTarihi.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(satir, 2).Value = k.Kullanici?.AdSoyad ?? "-";
                ws.Cell(satir, 3).Value = k.TezgahAdi ?? "-";
                ws.Cell(satir, 4).Value = k.Vardiya ?? "-";
                ws.Cell(satir, 5).Value = k.ParcaAdi ?? "-";
                ws.Cell(satir, 6).Value = k.HedefMiktar.HasValue ? k.HedefMiktar.Value : 0;
                ws.Cell(satir, 7).Value = toplam;
                ws.Cell(satir, 8).Value = k.SaglamMiktar;
                ws.Cell(satir, 9).Value = k.HurdaMiktar;
                ws.Cell(satir, 10).Value = k.ReworkMiktar;
                ws.Cell(satir, 11).Value = (k.CalismaBaslangic != null || k.CalismaBitis != null)
                    ? $"{k.CalismaBaslangic:hh\\:mm} - {k.CalismaBitis:hh\\:mm}"
                    : "Tam Vardiya";
                ws.Cell(satir, 12).Value = k.Aciklama ?? "-";
                satir++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var icerik = stream.ToArray();

            string dosyaAdi = $"Uretim_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(icerik, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", dosyaAdi);
        }

        // ==========================================
        // 4. PARÇA & HEDEF YÖNETİMİ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Parcalar()
        {
            var parcalar = await _db.Parcalar.OrderBy(p => p.ParcaAdi).ToListAsync();
            return View("~/Views/Muhendis/Parcalar.cshtml", parcalar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParcaEkle(string parcaAdi, int hedefSureDk, int hedefVardiya1, int hedefVardiya2, int hedefVardiya3)
        {
            if (string.IsNullOrWhiteSpace(parcaAdi))
            {
                TempData["Hata"] = "Parça adı boş bırakılamaz!";
                return RedirectToAction("Parcalar");
            }

            var yeniParca = new Parca
            {
                ParcaAdi = parcaAdi.Trim(),
                HedefSureDk = hedefSureDk,
                HedefMiktarVardiya1 = hedefVardiya1,
                HedefMiktarVardiya2 = hedefVardiya2,
                HedefMiktarVardiya3 = hedefVardiya3
            };

            _db.Parcalar.Add(yeniParca);
            await _db.SaveChangesAsync();

            TempData["Mesaj"] = $"{parcaAdi} başarıyla kaydedildi.";
            return RedirectToAction("Parcalar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParcaGuncelle(int id, string parcaAdi, int hedefSureDk, int hedefVardiya1, int hedefVardiya2, int hedefVardiya3)
        {
            if (string.IsNullOrWhiteSpace(parcaAdi))
            {
                TempData["Hata"] = "Parça adı boş bırakılamaz!";
                return RedirectToAction("Parcalar");
            }

            var parca = await _db.Parcalar.FindAsync(id);
            if (parca != null)
            {
                parca.ParcaAdi = parcaAdi.Trim();
                parca.HedefSureDk = hedefSureDk;
                parca.HedefMiktarVardiya1 = hedefVardiya1;
                parca.HedefMiktarVardiya2 = hedefVardiya2;
                parca.HedefMiktarVardiya3 = hedefVardiya3;

                await _db.SaveChangesAsync();
                TempData["Mesaj"] = $"{parca.ParcaAdi} bilgileri başarıyla güncellendi.";
            }

            return RedirectToAction("Parcalar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParcaSil(int id)
        {
            var parca = await _db.Parcalar.FindAsync(id);
            if (parca != null)
            {
                _db.Parcalar.Remove(parca);
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = $"{parca.ParcaAdi} silindi.";
            }
            return RedirectToAction("Parcalar");
        }

        // ==========================================
        // 5. TEZGAH YÖNETİMİ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Tezgahlar()
        {
            var tezgahlar = await _db.Tezgahlar.OrderBy(t => t.TezgahAdi).ToListAsync();
            return View("~/Views/Muhendis/Tezgahlar.cshtml", tezgahlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TezgahEkle(string tezgahAdi)
        {
            if (string.IsNullOrWhiteSpace(tezgahAdi))
            {
                TempData["Hata"] = "Tezgah adı boş bırakılamaz!";
                return RedirectToAction("Tezgahlar");
            }

            bool varMi = await _db.Tezgahlar.AnyAsync(t => t.TezgahAdi.ToLower() == tezgahAdi.Trim().ToLower());
            if (varMi)
            {
                TempData["Hata"] = "Bu tezgah zaten kayıtlı!";
                return RedirectToAction("Tezgahlar");
            }

            _db.Tezgahlar.Add(new Tezgah { TezgahAdi = tezgahAdi.Trim() });
            await _db.SaveChangesAsync();

            TempData["Mesaj"] = $"{tezgahAdi} başarıyla eklendi.";
            return RedirectToAction("Tezgahlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TezgahGuncelle(int id, string tezgahAdi)
        {
            if (string.IsNullOrWhiteSpace(tezgahAdi))
            {
                TempData["Hata"] = "Tezgah adı boş bırakılamaz!";
                return RedirectToAction("Tezgahlar");
            }

            var tezgah = await _db.Tezgahlar.FindAsync(id);
            if (tezgah != null)
            {
                tezgah.TezgahAdi = tezgahAdi.Trim();
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = "Tezgah adı başarıyla güncellendi.";
            }

            return RedirectToAction("Tezgahlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TezgahSil(int id)
        {
            var tezgah = await _db.Tezgahlar.FindAsync(id);
            if (tezgah != null)
            {
                _db.Tezgahlar.Remove(tezgah);
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = $"{tezgah.TezgahAdi} silindi.";
            }
            return RedirectToAction("Tezgahlar");
        }

        // ==========================================
        // 6. PERSONEL YÖNETİMİ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Personeller()
        {
            var personeller = await _db.Kullanicilar
                .Where(k => k.Rol != "Muhendis")
                .OrderBy(k => k.AdSoyad)
                .ToListAsync();

            return View("~/Views/Muhendis/Personeller.cshtml", personeller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelEkle(string adSoyad, string kullaniciAdi, string sifre)
        {
            if (string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                TempData["Hata"] = "Lütfen tüm personel alanlarını doldurunuz!";
                return RedirectToAction("Personeller");
            }

            bool varMi = await _db.Kullanicilar.AnyAsync(k => k.KullaniciAdi.ToLower() == kullaniciAdi.Trim().ToLower());
            if (varMi)
            {
                TempData["Hata"] = "Bu kullanıcı adı zaten kullanılmaktadır!";
                return RedirectToAction("Personeller");
            }

            var yeniPersonel = new Kullanici
            {
                AdSoyad = adSoyad.Trim(),
                KullaniciAdi = kullaniciAdi.Trim(),
                SifreHash = BCrypt.Net.BCrypt.HashPassword(sifre.Trim()),
                Rol = "Personel"
            };

            _db.Kullanicilar.Add(yeniPersonel);
            await _db.SaveChangesAsync();

            TempData["Mesaj"] = $"{adSoyad} sisteme başarıyla eklendi.";
            return RedirectToAction("Personeller");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelSil(int id)
        {
            var personel = await _db.Kullanicilar.FindAsync(id);
            if (personel != null && personel.Rol != "Muhendis")
            {
                _db.Kullanicilar.Remove(personel);
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = $"{personel.AdSoyad} silindi.";
            }
            return RedirectToAction("Personeller");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreDegistir(string guvenlikKodu, string yeniSifre, string yeniSifreTekrar)
        {
            // Özel Güvenlik Şifresi Kontrolü
            const string BEKLENEN_KOD = "Sbd6606";

            if (guvenlikKodu != BEKLENEN_KOD)
            {
                TempData["Hata"] = "Hatalı Güvenlik Şifresi! Şifre değiştirilemedi.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 4)
            {
                TempData["Hata"] = "Yeni şifre en az 4 karakter olmalıdır!";
                return RedirectToAction("Index");
            }

            if (yeniSifre != yeniSifreTekrar)
            {
                TempData["Hata"] = "Girdiğiniz yeni şifreler birbiriyle uyuşmuyor!";
                return RedirectToAction("Index");
            }

            // Giriş yapan mühendisi veya admin hesabını bul
            var kullaniciAdi = User.Identity?.Name ?? "admin";
            var admin = await _db.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi || k.Rol == "Muhendis");

            if (admin != null)
            {
                admin.SifreHash = BCrypt.Net.BCrypt.HashPassword(yeniSifre.Trim());
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = "Mühendis (Admin) şifreniz başarıyla değiştirildi.";
            }
            else
            {
                TempData["Hata"] = "Kullanıcı hesabı bulunamadı!";
            }

            return RedirectToAction("Index");
        }


    }
}