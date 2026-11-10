using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AtolyeTakip.Data;

namespace AtolyeTakip.Controllers
{
    public class HesapController : Controller
    {
        private readonly AtolyeDbContext _db;

        public HesapController(AtolyeDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Giris()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Giris(string kullaniciAdi, string sifre)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Lütfen kullanıcı adı ve şifre giriniz!";
                return View();
            }

            var girilenKullanici = kullaniciAdi.Trim();
            var kullanici = await _db.Kullanicilar
                .FirstOrDefaultAsync(u => u.KullaniciAdi.ToLower() == girilenKullanici.ToLower());

            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(sifre.Trim(), kullanici.SifreHash))
            {
                ViewBag.Hata = "Kullanıcı adı veya şifre hatalı!";
                return View();
            }

            // Türkçe karakter uyumluluğu (Mühendis / Muhendis)
            bool isMuhendis = string.Equals(kullanici.Rol, "Muhendis", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(kullanici.Rol, "Mühendis", StringComparison.OrdinalIgnoreCase);

            string rol = isMuhendis ? "Muhendis" : "Personel";

            var roller = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Name, kullanici.AdSoyad),
                new Claim(ClaimTypes.Role, rol)
            };

            var kimlik = new ClaimsIdentity(roller, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(kimlik),
                authProperties
            );

            if (isMuhendis)
            {
                return RedirectToAction("Index", "Muhendis");
            }

            return RedirectToAction("Index", "Uretim");
        }

        [HttpGet]
        public async Task<IActionResult> Cikis()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Giris");
        }
    }
}