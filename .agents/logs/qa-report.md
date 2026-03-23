# QA Report — VOLK Sprint
> Tarih: 2026-03-23
> Kontrol eden: Claude DEV Agent
> Kontrol edilen dosya sayisi: 42 C# dosyasi

---

## KRITIK HATALAR

### BUG-01: DialogueManager intro/outro ayrimi yapmiyor
- **Dosya:** `Scripts/Story/DialogueManager.cs:31,47`
- **Hata:** `isIntro` field'i her sahne yuklendiginde `true` olarak basliyor. StoryManager hem intro hem outro icin ayni "Dialogue" sahnesini yukluyor. Outro gosterilmesi gereken durumda bile intro diyalogu tekrar gosteriliyor.
- **Etki:** Hikaye modunda chapter kazanildiktan sonra outro diyalogu ASLA gosterilmiyor.
- **Onem:** KRITIK
- **Cozum:** StoryManager'a `showOutro` flag'i ekle, DialogueManager bunu okusun.

---

## ORTA HATALAR

### BUG-02: LeaderboardManager PlayerRank hesaplanmiyor
- **Dosya:** `Scripts/Meta/LeaderboardManager.cs:84-91`
- **Hata:** For loop'un icinde player_id karsilastirmasi yapilmiyor. PlayerRank her zaman -1 olarak kaliyor.
- **Etki:** Oyuncunun kendi sirasi UI'da asla gosterilmiyor.
- **Onem:** ORTA
- **Cozum:** SupabaseManager'dan playerId expose et, for loop'ta karsilastir.

### BUG-03: SupabaseManager playerId private
- **Dosya:** `Scripts/Meta/SupabaseManager.cs:19`
- **Hata:** `playerId` field'i private. LeaderboardManager player rank hesaplamak icin buna erisemiyor.
- **Etki:** BUG-02'nin root cause'u.
- **Onem:** ORTA
- **Cozum:** `public string PlayerId => playerId;` property ekle.

### BUG-04: CharacterUnlockManager ve SaveManager dual-source tutarsizligi
- **Dosya:** `Scripts/Core/CharacterUnlockManager.cs:19,29,34,39,53`
- **Hata:** CharacterUnlockManager dogrudan PlayerPrefs kullaniyor (`char_unlocked_X`, `total_wins`, `currency`). SaveManager ise `volk_save_data` JSON key'inde ayni verileri tutuyor. Iki sistem birbirinden habersiz.
- **Etki:** SaveManager ile karakter acildiginda CharacterUnlockManager bunu bilmiyor ve tersi.
- **Onem:** ORTA
- **Cozum:** CharacterUnlockManager'i SaveManager'a delegate et.

### BUG-05: MainMenuController serialized field isim degisikligi
- **Dosya:** `Scripts/MainMenuController.cs:11`
- **Hata:** `combatSceneName` → `nextSceneName` olarak yeniden adlandirildi. Unity sahnede serialize edilmis eski deger kaybolur, Inspector'da "CharacterSelect" yerine bos/eski deger gorunebilir.
- **Etki:** MainMenu'de PLAY butonuna basilinca yanlis sahneye gidebilir veya hata verebilir.
- **Onem:** ORTA
- **Cozum:** `FormerlySerializedAs` attribute ekle.

---

## DUSUK HATALAR

### BUG-06: SO asset dosyalari henuz olusturulmamis
- **Dosya:** `Assets/ScriptableObjects/` (tum klasorler)
- **Hata:** .asset dosyalari yok, sadece .gitkeep var. Editor script'leri (CreateCharacterAssets, CreateChapterAssets, CreateComboAssets, CreateQuestAssets) Unity icinde calistirilmali.
- **Onem:** DUSUK (beklenen — Unity Editor gerekli)

### BUG-07: Eksik sahneler
- **Dosya:** `Assets/Scenes/`
- **Hata:** CharacterSelect, Dialogue, StoryMenu, Shop, Leaderboard sahneleri henuz olusturulmamis.
- **Onem:** DUSUK (Unity Editor'da olusturulacak)

### BUG-08: Feature branch'lar temizlenmemis
- **Hata:** 16 feature branch hala mevcut (hepsi dev'e merge edilmis).
- **Onem:** DUSUK

### BUG-09: GameManager WinWithoutDamage kontrolu sadece son round'u kontrol ediyor
- **Dosya:** `Scripts/GameManager.cs:185`
- **Hata:** `playerFighter.currentHP >= playerFighter.maxHP` sadece son round sonrasindaki HP'yi kontrol ediyor. Onceki round'larda hasar alinip alinmadigini bilmiyor.
- **Onem:** DUSUK (quest mantigi olarak kabul edilebilir)

---

## BUTUNLUK KONTROLU SONUCLARI

| Kontrol | Sonuc |
|---------|-------|
| Fighter.cs public API | OK — tum mevcut metodlar korunmus, yeni eklenenler geriye uyumlu |
| GameManager.cs | OK — round sistemi bozulmamis, yeni hook'lar null-safe |
| TouchInputHandler.cs | OK — dokunulmamis |
| FightButton.cs | OK — dokunulmamis |
| TouchCombatBridge.cs | OK — sadece double tap → UseSkill degisikligi |
| Physics Layer 8/9 | OK — dokunulmamis |
| MainMenu sahne akisi | UYARI — field rename (BUG-05) |
| AudioManager | OK — geriye uyumlu, yeni PlayBlock eklendi |
| VibrationManager | OK — dokunulmamis |
| PauseMenu | OK — dokunulmamis |
| HealthBarUI | OK — dokunulmamis |
| CameraFollow | OK — dokunulmamis |

## SUPABASE KONTROLU

| Kontrol | Sonuc |
|---------|-------|
| URL | OK — `https://ktiyviyypeuutvtfkjnu.supabase.co` config.env ile eslesir |
| Anon Key | OK — `sb_publishable_sNXopE96fnyPfK7iQqeT9w_Yask869v` config.env ile eslesir |
| Auth endpoint | OK — `/auth/v1/signup`, `/auth/v1/token?grant_type=password` |
| REST endpoints | OK — `/rest/v1/players`, `/rest/v1/save_data`, `/rest/v1/leaderboard` |
| Headers | OK — apikey, Authorization Bearer, Content-Type, Prefer |
| SQL schema | OK — `.agents/supabase-schema.sql` RLS ve index'ler mevcut |
