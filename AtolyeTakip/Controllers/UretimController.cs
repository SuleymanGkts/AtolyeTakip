using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AtolyeTakip.Data;
using AtolyeTakip.Models;

namespace AtolyeTakip.Controllers
{
    // Ustalar, Personeller ve Mühendisler erişebilir
    [Authorize(Roles = "Personel,Usta,Muhendis,Mühendis")]
    public class UretimController : Controller
    {
        private readonly AtolyeDbContext _db;

        public UretimController(AtolyeDbContext db)
        {
            _db = db;
        }

        // 1. Sayfa Açılışı (Tezgahlar ve Parçalar dropdown için ViewBag'e yüklenir)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await SayfaVerileriniYukleAsync();
            return View();
        }

        // 2. Form Kayıt İşlemi (/Uretim/Kaydet isteği için)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kaydet(
            string vardiya,
            string tezgahAdi,
            string parcaSecim,
            string? manuelParcaAdi,
            int? saglamMiktar,
            int? hurdaMiktar,
            int? reworkMiktar,
            TimeSpan? calismaBaslangic,
            TimeSpan? calismaBitis,
            string? aciklama)
        {
            int saglam = saglamMiktar ?? 0;
            int hurda = hurdaMiktar ?? 0;
            int rework = reworkMiktar ?? 0;

            // KONTROL 1: Vardiya seçilmiş mi?
            if (string.IsNullOrWhiteSpace(vardiya))
            {
                ModelState.AddModelError("", "Lütfen önce bir vardiya seçiniz!");
                await SayfaVerileriniYukleAsync();
                return View("Index");
            }

            // KONTROL 2: Tezgah seçilmiş mi?
            if (string.IsNullOrWhiteSpace(tezgahAdi))
            {
                ModelState.AddModelError("", "Lütfen çalıştığınız tezgahı seçiniz!");
                await SayfaVerileriniYukleAsync();
                return View("Index");
            }

            // KONTROL 3: Parça seçilmiş mi?
            if (string.IsNullOrWhiteSpace(parcaSecim))
            {
                ModelState.AddModelError("", "Lütfen işlediğiniz parçayı seçiniz!");
                await SayfaVerileriniYukleAsync();
                return View("Index");
            }

            // KONTROL 4: Standart dışı iş seçildiyse isim yazılmış mı?
            if (parcaSecim == "MANUEL" && string.IsNullOrWhiteSpace(manuelParcaAdi))
            {
                ModelState.AddModelError("", "Lütfen standart dışı iş için parça / aparat adını yazınız!");
                await SayfaVerileriniYukleAsync();
                return View("Index");
            }

            // KONTROL 5: Üretim miktarı boş veya 0 olamaz!
            if ((saglam + hurda + rework) <= 0)
            {
                ModelState.AddModelError("", "Üretim miktarı boş veya 0 olamaz! En az bir miktar (Sağlam, Hurda veya Rework) girilmelidir.");
                await SayfaVerileriniYukleAsync();
                return View("Index");
            }

            // KONTROL 6: Çalışma saatleri girildiyse açıklama zorunludur!
            bool saatGirildi = calismaBaslangic.HasValue || calismaBitis.HasValue;
            if (saatGirildi && string.IsNullOrWhiteSpace(aciklama))
            {
                ModelState.AddModelError("", "Çalışma saatlerini değiştirdiğiniz için açıklama girilmesi zorunludur!");
                await SayfaVerileriniYukleAsync();
                return View("Index");
            }

            // Parça ve Hedef Bilgilerini Çekme
            string secilenParcaAdi = "";
            int? hedefSure = null;
            int? hedefMiktar = null;

            if (parcaSecim == "MANUEL")
            {
                secilenParcaAdi = manuelParcaAdi!.Trim();
            }
            else
            {
                if (int.TryParse(parcaSecim, out int parcaId))
                {
                    var parca = await _db.Parcalar.FindAsync(parcaId);
                    if (parca != null)
                    {
                        secilenParcaAdi = parca.ParcaAdi;
                        hedefSure = parca.HedefSureDk;

                        // Seçilen vardiyaya ait hedef miktarı ata
                        if (vardiya.Contains("1. Vard"))
                            hedefMiktar = parca.HedefMiktarVardiya1;
                        else if (vardiya.Contains("2. Vard"))
                            hedefMiktar = parca.HedefMiktarVardiya2;
                        else if (vardiya.Contains("3. Vard"))
                            hedefMiktar = parca.HedefMiktarVardiya3;
                    }
                }
            }

            // Giriş Yapan Kullanıcının Kimliğini Belirleme
            int kullaniciId = 1;
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claimId, out int parsedId))
            {
                kullaniciId = parsedId;
            }
            else if (!string.IsNullOrEmpty(User.Identity?.Name))
            {
                var dbKullanici = await _db.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == User.Identity.Name);
                if (dbKullanici != null)
                    kullaniciId = dbKullanici.Id;
            }

            // Kaydı Oluşturma ve Kaydetme
            var yeniKayit = new UretimKaydi
            {
                KullaniciId = kullaniciId,
                TezgahAdi = tezgahAdi.Trim(),
                ParcaAdi = secilenParcaAdi,
                HedefSureDk = hedefSure,
                HedefMiktar = hedefMiktar,
                SaglamMiktar = saglam,
                HurdaMiktar = hurda,
                ReworkMiktar = rework,
                Vardiya = vardiya,
                CalismaBaslangic = calismaBaslangic,
                CalismaBitis = calismaBitis,
                Aciklama = aciklama?.Trim(),
                KayitTarihi = DateTime.Now
            };

            _db.UretimKayitlari.Add(yeniKayit);
            await _db.SaveChangesAsync();

            TempData["Basari"] = "Üretim kaydınız başarıyla kaydedildi.";
            TempData["Mesaj"] = "Üretim kaydınız başarıyla kaydedildi.";
            return RedirectToAction("Index");
        }

        // Form doğrudan /Uretim/Index adresine POST edilirse de Kaydet aksiyonunu çalıştırır
        [HttpPost]
        [ActionName("Index")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IndexPost(
            string vardiya,
            string tezgahAdi,
            string parcaSecim,
            string? manuelParcaAdi,
            int? saglamMiktar,
            int? hurdaMiktar,
            int? reworkMiktar,
            TimeSpan? calismaBaslangic,
            TimeSpan? calismaBitis,
            string? aciklama)
        {
            return await Kaydet(vardiya, tezgahAdi, parcaSecim, manuelParcaAdi, saglamMiktar, hurdaMiktar, reworkMiktar, calismaBaslangic, calismaBitis, aciklama);
        }

        // Ortak Dropdown Doldurma Yardımcısı
        private async Task SayfaVerileriniYukleAsync()
        {
            ViewBag.Tezgahlar = await _db.Tezgahlar.OrderBy(t => t.TezgahAdi).ToListAsync();
            ViewBag.Parcalar = await _db.Parcalar.OrderBy(p => p.ParcaAdi).ToListAsync();
        }
    }
}