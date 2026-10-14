using WebApi.Models;

namespace WebApi.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Ha már vannak felhasználók, feltételezzük, hogy az adatbázis már fel van töltve
            if (context.Felhasznalok.Any()) return;

            var rnd = new Random();

            // 1. FELHASZNÁLÓK
            var admin = new Felhasznalo { Nev = "Rendszer Admin", Email = "admin@uni.hu", Jelszo = "titkos123", Szerepkor = "Adminisztrator" };
            var uzemelteto = new Felhasznalo { Nev = "Kovács Üzemeltető", Email = "uzemeltetes@uni.hu", Jelszo = "titkos123", Szerepkor = "UzemeltetesiIgazgato" };
            var portas = new Felhasznalo { Nev = "Nagy Portás", Email = "portas@uni.hu", Jelszo = "titkos123", Szerepkor = "Portas" };

            var oktato1 = new Felhasznalo { Nev = "Dr. Kiss Oktató", Email = "kiss.o@uni.hu", Jelszo = "titkos123", Szerepkor = "Oktato" };
            var oktato2 = new Felhasznalo { Nev = "Dr. Szabó Oktató", Email = "szabo.o@uni.hu", Jelszo = "titkos123", Szerepkor = "Oktato" };
            var oktato3 = new Felhasznalo { Nev = "Dr. Tóth Oktató", Email = "toth.o@uni.hu", Jelszo = "titkos123", Szerepkor = "Oktato" };

            context.Felhasznalok.AddRange(admin, uzemelteto, portas, oktato1, oktato2, oktato3);

            // 2. FELSZERELTSÉGEK
            var projektor = new Felszereltseg { Nev = "Projektor" };
            var okostabla = new Felszereltseg { Nev = "Okostábla" };
            var filctabla = new Felszereltseg { Nev = "Filctábla" };
            var szamitogepek = new Felszereltseg { Nev = "Számítógépek" };
            var labor = new Felszereltseg { Nev = "Labor" };

            context.AddRange(projektor, okostabla, filctabla, szamitogepek, labor);

            // 3. TERMEK ÉS KULCSOK GENERÁLÁSA
            var termek = new List<Terem>();
            var kulcsok = new List<Kulcs>();

            void CreateTerem(string nev, bool isPcRoom)
            {
                var terem = new Terem
                {
                    Teremszam = nev,
                    Ferohely = rnd.Next(20, 51),
                    Adminisztrator = admin,
                    Felszereltsegek = new List<Felszereltseg> { filctabla } // Filctábla mindenhol van
                };

                if (isPcRoom) terem.Felszereltsegek.Add(szamitogepek);
                if (rnd.NextDouble() > 0.5) terem.Felszereltsegek.Add(projektor);
                if (rnd.NextDouble() > 0.8) terem.Felszereltsegek.Add(okostabla);

                termek.Add(terem);
                kulcsok.Add(new Kulcs { Terem = terem, MesterkulcsE = false, KiadvaE = false });
            }

            // Földszint
            CreateTerem("I1", false);
            CreateTerem("I2", false);
            CreateTerem("I3", false);
            CreateTerem("PC0", true);

            // 1-9. emelet (101-120... 901-920)
            for (int emelet = 1; emelet <= 9; emelet++)
            {
                for (int szoba = 1; szoba <= 20; szoba++)
                {
                    string teremNev = $"I{emelet}{szoba:D2}";
                    CreateTerem(teremNev, szoba % 5 == 0); // Minden 5. terem legyen "gépterem"
                }
            }
            context.Termek.AddRange(termek);
            context.Kulcsok.AddRange(kulcsok);

            // 4. MESTERKULCSOK
            context.Kulcsok.Add(new Kulcs { MesterkulcsE = true, KiadvaE = false, Terem = null });
            context.Kulcsok.Add(new Kulcs { MesterkulcsE = true, KiadvaE = false, Terem = null });

            context.SaveChanges(); // Elmentjük, hogy legyen ID-juk a továbbiakhoz

            // 5. HIBÁK BEJELENTÉSE ÉS NAPLÓZÁSA
            var i1Terem = context.Termek.First(t => t.Teremszam == "I1");
            var pc0Terem = context.Termek.First(t => t.Teremszam == "PC0");

            context.Hibak.Add(new Hiba { Terem = i1Terem, Portas = portas, Leiras = "Kiégett a lámpa.", Allapot = false });
            context.Hibak.Add(new Hiba { Terem = pc0Terem, Portas = portas, Leiras = "Nem működik az egyik gép.", Allapot = false });

            context.AuditLogok.Add(new AuditLog { Muvelet = "Hiba", Reszletek = "Hiba bejelentve (I1): Kiégett a lámpa.", UzemeltetesiIgazgato = uzemelteto });
            context.AuditLogok.Add(new AuditLog { Muvelet = "Hiba", Reszletek = "Hiba bejelentve (PC0): Nem működik az egyik gép.", UzemeltetesiIgazgato = uzemelteto });

            // 6. FOGLALÁS ÉS NAPLÓZÁS
            var holnapDél = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
            var foglalas = new Foglalas
            {
                Oktato = oktato1,
                Terem = i1Terem,
                KezdoIdo = holnapDél,
                ZaroIdo = holnapDél.AddHours(1.5),
                Allapot = "Aktiv",
                IgenyeltFelszereltsegek = new List<Felszereltseg> { projektor }
            };
            context.Foglalasok.Add(foglalas);

            context.AuditLogok.Add(new AuditLog { Foglalas = foglalas, Muvelet = "Foglalas", Reszletek = $"Foglalás történt: {oktato1.Nev} lefoglalta az I1 termet holnap 12:00-13:30-ig.", UzemeltetesiIgazgato = uzemelteto });

            // 7. KULCSMOZGÁSOK (Elvitel és Visszahozatal)
            var i1Kulcs = context.Kulcsok.First(k => k.TeremID == i1Terem.TeremID);
            var elvitelIdo = DateTime.UtcNow.AddHours(-2);
            var visszahozasIdo = DateTime.UtcNow.AddMinutes(-30);

            var mozgasKi = new KulcsMozgas { Kulcs = i1Kulcs, Atvevo = oktato1, Portas = portas, Idobelyeg = elvitelIdo };
            var mozgasBe = new KulcsMozgas { Kulcs = i1Kulcs, Atvevo = oktato1, Portas = portas, Idobelyeg = visszahozasIdo };
            context.KulcsMozgasok.AddRange(mozgasKi, mozgasBe);

            context.AuditLogok.Add(new AuditLog { KulcsMozgas = mozgasKi, Muvelet = "KulcsMozgas", Reszletek = $"{oktato1.Nev} elvitte az I1 terem kulcsát.", UzemeltetesiIgazgato = uzemelteto });
            context.AuditLogok.Add(new AuditLog { KulcsMozgas = mozgasBe, Muvelet = "KulcsMozgas", Reszletek = $"{oktato1.Nev} visszahozta az I1 terem kulcsát másfél órával később.", UzemeltetesiIgazgato = uzemelteto });

            context.SaveChanges();
        }
    }
}