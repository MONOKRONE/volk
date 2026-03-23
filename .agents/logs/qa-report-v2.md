# QA Report v2 — VOLK Sprint 2
> Tarih: 2026-03-23
> Kontrol edilen dosya: 63 C# dosyasi (yeni + degistirilen)
> Sprint: PLA-45 → PLA-56

---

## KRITIK HATALAR

Yok.

---

## ORTA HATALAR

### BUG-V2-01: ArenaManager wall material memory leak
- **Dosya:** `Scripts/Core/ArenaManager.cs:63-72, 220-221`
- **Hata:** `ApplyWalls()` her duvar icin yeni Material olusturuyor ama bunlari `wallMats` array'ine kaydetmiyor. `OnDestroy()` `wallMats` uzerinden cleanup yapmaya calisiyor ama wallMats her zaman null. Olusturulan duvar material'leri asla temizlenmiyor.
- **Etki:** Her arena degisikliginde memory leak.
- **Onem:** ORTA
- **Cozum:** wallMats array'ini ApplyWalls'da doldurmak.

### BUG-V2-02: SurvivalManager XP kazandirmiyor
- **Dosya:** `Scripts/Core/SurvivalManager.cs:102-121`
- **Hata:** `OnPlayerDefeated()` sadece totalMatches kaydediyor ama `LevelSystem.AddSurvivalXP(CurrentRound)` cagirmiyor.
- **Etki:** Survival modunda oynayan oyuncu XP kazanmiyor.
- **Onem:** ORTA
- **Cozum:** OnPlayerDefeated()'a LevelSystem.AddSurvivalXP() ekle.

### BUG-V2-03: MatchStatsTracker baglantisi yok
- **Dosya:** `Scripts/Core/MatchStats.cs:92-150`
- **Hata:** MatchStatsTracker var ama Fighter.cs'deki saldiri/hasar metotlarindan cagrilmiyor. RecordHitLanded, RecordHitReceived, FinalizeMatch hicbir yerde tetiklenmiyor.
- **Etki:** MatchEndUI ve VictoryDefeatUI her zaman sifir istatistik gosterir.
- **Onem:** ORTA
- **Cozum:** Fighter.cs'in DoAttack ve TakeDamage metotlarinda MatchStatsTracker'a bildir.

### BUG-V2-04: Chapter grade'leri kaydedilmiyor
- **Dosya:** `Scripts/UI/LevelMapUI.cs:107,156`
- **Hata:** `PlayerPrefs.GetString($"chapter_{i}_grade")` okunuyor ama hicbir yerde yazilmiyor. MatchStats grade hesapliyor ama StoryManager.OnChapterWon()'da grade kaydedilmiyor.
- **Etki:** Level haritasinda yildiz derecesi hep "C" (default).
- **Onem:** ORTA
- **Cozum:** StoryManager.OnChapterWon()'da grade'i PlayerPrefs'e kaydet.

### BUG-V2-05: QuickFightUI geri butonu yanlis sahneye gidiyor
- **Dosya:** `Scripts/UI/QuickFightUI.cs:73`
- **Hata:** Back button `SceneManager.LoadScene("MainMenu")` ama yeni akis MainHub kullanıyor.
- **Etki:** Serbest dovus'ten geri donunce eski MainMenu aciliyor, MainHub degil.
- **Onem:** ORTA
- **Cozum:** "MainMenu" → "MainHub" degistir.

---

## DUSUK HATALAR

### BUG-V2-06: SurvivalManager ve TrainingManager gereksiz using
- **Dosya:** `Scripts/Core/SurvivalManager.cs:4`, `Scripts/Core/TrainingManager.cs:2`
- **Hata:** `using Volk.Core;` yazilmis ama dosya zaten `namespace Volk.Core` icinde. Redundant using.
- **Onem:** DUSUK

### BUG-V2-07: MatchEndUI ve VictoryDefeatUI duplikasyon
- **Dosya:** `Scripts/UI/MatchEndUI.cs`, `Scripts/UI/VictoryDefeatUI.cs`
- **Hata:** Iki ayri mac sonu UI'i var. VictoryDefeatUI daha kapsamli (coin rain, level up). MatchEndUI daha basit.
- **Onem:** DUSUK (ikisi farkli sahnelerde kullanilabilir)

### BUG-V2-08: Font dosyalari mevcut degil
- **Dosya:** `Assets/UI/Fonts/`
- **Hata:** Rajdhani ve Inter font dosyalari indirilmemis. TMP SDF asset olusturulmamis.
- **Onem:** DUSUK (manuel indirme gerekli)

### BUG-V2-09: SurvivalManager kullanilmayan using
- **Dosya:** `Scripts/Core/SurvivalManager.cs:2`
- **Hata:** `using UnityEngine.SceneManagement;` import edilmis ama sinif icinde kullanilmiyor.
- **Onem:** DUSUK

---

## BUTUNLUK KONTROLU

| Kontrol | Sonuc |
|---------|-------|
| Fighter.cs public API | OK — dokunulmamis (sadece XP entegrasyonu GameManager'da) |
| GameManager.cs | OK — QuickFight difficulty + XP + MatchStats eklendi, mevcut round sistemi korundu |
| Physics Layer 8/9 | OK — dokunulmamis |
| Input sistemi (legacy) | OK — dokunulmamis |
| TouchInputHandler | OK — dokunulmamis |
| FightButton | OK — dokunulmamis |
| TouchCombatBridge | OK — Sprint 1 degisiklikleri korundu |
| MainMenu sahnesi | OK — hala calisir (eski akis korundu) |
| CombatTest sahnesi | OK — hala calisir |

## UI SISTEM KONTROLU

| Kontrol | Sonuc |
|---------|-------|
| VTheme renk paleti | OK — #0A0A14, #1A1A2E, #E94560, #FFD700, #00D4FF dogru |
| VButton PunchScale | OK — 0.92→1.08 animasyon mevcut |
| VTopBar | OK — avatar, level, XP bar, coin gostergesi mevcut |
| VTabBar | OK — 5 tab, aktif tab Red vurgusu |
| Font dosyalari | EKSIK — Downloads'ta bulunamadi |

## ARENA KONTROLU

| Kontrol | Sonuc |
|---------|-------|
| ArenaData SO | OK — CreateAssetMenu attribute, tum field'lar mevcut |
| ArenaManager | MEMORY LEAK — BUG-V2-01 |
| 5 arena preset | OK — Sokak, Yeralti, Cati, Fabrika, Bogaz (editor script) |
| ChapterData.arenaData | OK — field eklenmis |

## MOD KONTROLU

| Kontrol | Sonuc |
|---------|-------|
| QuickFightUI | OK — karakter + rakip + arena + zorluk secimi tam |
| SurvivalManager | ORTA BUG — XP kazandirmiyor (BUG-V2-02) |
| TrainingManager | OK — sonsuz HP, AI toggle, DPS tracker |

## GIT KONTROLU

| Kontrol | Sonuc |
|---------|-------|
| Commit'ler | OK — 12 feature commit + 12 merge commit |
| Branch'lar | OK — tum feature branch'lar silinmis |
| Orphan branch | Yok |
