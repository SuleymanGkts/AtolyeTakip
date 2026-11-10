ALTER TABLE UretimKayitlari ADD Aciklama NVARCHAR(500) NULL;
ALTER TABLE UretimKayitlari ADD IslemeSuresiDakika INT NOT NULL DEFAULT 0;
ALTER TABLE UretimKayitlari ADD IslemeSuresiSaniye INT NOT NULL DEFAULT 0;
