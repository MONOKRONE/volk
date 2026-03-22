# VOLK — Proje Durum Belgesi
> Son güncelleme: Mart 2026

---

## Proje Bilgileri

| | |
|---|---|
| **Engine** | Unity 2022.3.62f1 + URP |
| **Platform** | Android (önce), iOS (Phase 7) |
| **Repo** | github.com/MONOKRONE/volk |
| **Local path** | ~/Developer/volk/Volk/ |
| **Unity MCP** | Port 8090 |
| **Git config** | MONOKRONE / cirakcibugra@gmail.com |
| **Bundle ID** | com.monokrone.volk |
| **Min API** | Android 7.0 (API 24) |
| **Build Backend** | IL2CPP, ARM64 |

---

## ✅ Phase 0 — Combat Prototype (TAMAMLANDI)

### Sahne: CombatTest
- Arena (Plane + 4 duvar)
- Player_Root → Maria.fbx (Mixamo)
- Enemy_Root → Kachujin.fbx (Mixamo)
- CameraFollow.cs — oyuncunun arkasından takip
- HealthCanvas — HP barlar (smooth lerp)
- GameManager — singleton
- TouchCanvas — 5 buton

### Karakterler & Animasyonlar (Assets/Characters/, Assets/Animations/)
- Maria.fbx (oyuncu), Kachujin.fbx (düşman)
- Animasyonlar: Idle, Walk, Run, HookPunch, MMAKick, BodyBlock, TakingPunch, ReceivingUppercut, Death, Jump

### Scripts
| Script | Açıklama |
|--------|----------|
| Fighter.cs | Ana karakter kontrolcüsü (oyuncu + AI) |
| TouchInputHandler.cs | Input.touches ile joystick (legacy API) |
| FightButton.cs | Tap/hold/doubletap/slide gesture |
| TouchCombatBridge.cs | Buton → Fighter bağlantısı |
| CameraFollow.cs | Lock-on + free look kamera |
| HealthBarUI.cs | HP bar UI |
| GameManager.cs | Singleton, round yönetimi |

### Physics Layer
- Default (0): Player_Root, Enemy_Root — zemin çarpışması için
- Hurtbox (9): Hasar alınan alan
- Hitbox (8): Saldırı alanı
- Matrix: Hitbox↔Hurtbox ON

### CLAUDE.md Kuralları (repoda mevcut)
- Her dosya değişikliğinde commit + push
- Açıklayıcı commit mesajları (type: description)
- Asla `ScrollTrigger.getAll().forEach(t=>t.kill())` kullanma
- Her prompt tek görev

---

## ✅ Phase 1 — Polish & Feel (TAMAMLANDI)

| Özellik | Detay |
|---------|-------|
| **Jump** | Joystick flick up → yVelocity formülü, Jump animasyon |
| **Crouch** | Joystick flick down → CC height küçülür, toggle |
| **Hit Stop** | Vuruşta 0.08s animator.speed=0 (unscaled time) |
| **Camera Shake** | Hasar alınca 0.1s magnitude 0.05f |
| **Knockback** | Saldırgan yönünden uzaklaşma, lerp deceleration, force=2f |
| **Combo Window** | Vuruş land'lince 0.4s sonraki attack beklemez |

---

## ✅ Phase 2 — UI & Juice (TAMAMLANDI)

### Tur Sistemi (GameManager.cs + RoundUI.cs)
- Best of 3 round sistemi
- "ROUND X" → "FIGHT!" intro animasyonu (PunchScale coroutine)
- 99 saniye geri sayım (kırmızıya döner ≤10s)
- Round win dots (2 adet, oyuncu + düşman)
- "K.O." veya "TIME" sonuç ekranı
- "YOU WIN" / "YOU LOSE" maç sonu ekranı
- Tap to restart
- Fighter.ResetForRound() — HP, animasyon, velocity sıfırlama

### Hit Particle Efektleri
- Hit Impact Effects FREE (Travis Game Assets) import edildi
- HitEffectManager.cs — punch/kick/block ayrı prefab
- Spawn: saldırı land'lince, göğüs hizasında

### Ses Sistemi (AudioManager.cs)
- AudioClip[] array — random varyasyon desteği
- PlayPunch(), PlayKick() — sadece hasar verilince
- PlayHit() — hasar alınca
- PlayFall() — ölüm anında
- PlayRoundStart() — ilk 3 saniye (PlayClipLimited coroutine)
- PlayerPrefs ile ses açık/kapalı kayıt

### Ana Menü (MainMenu sahnesi)
- İki karakter karşılıklı idle animasyonla
- "VOLK" + "FIGHT." tagline
- PLAY butonu → CombatTest sahnesine fade geçiş
- CameraMenuDrift.cs — sinüs dalgası kamera hareketi
- MainMenuController.cs — fade in/out, landscape lock

### Pause Menü (PauseMenu.cs + PauseMenuBuilder.cs)
- Procedural olarak C# ile inşa edildi (2 kolon layout)
- Sol kolon: Devam Et, Yeniden Başlat, Ses toggle, Titreşim toggle
- Sağ kolon: Zorluk seviyesi (Kolay/Normal/Zor)
- Android back button → pause toggle
- Time.timeScale = 0 (unscaled coroutine)
- PlayerPrefs ile ses + titreşim + zorluk kayıt

### Titreşim (VibrationManager.cs)
- VibrateLight(): hasar verince + hasar alınca
- VibrateHeavy(): KO anında
- PlayerPrefs ile kayıt

---

## ✅ Phase 3 — AI İyileştirme (TAMAMLANDI)

### FSM Tabanlı AI
```
Idle → Approach → Combat → Retreat → Stunned
```

| State | Koşul |
|-------|-------|
| Idle | Round başlangıcı, kısa bekleme |
| Approach | Mesafe > 1.8f |
| Combat | Mesafe ≤ 1.8f |
| Retreat | HP < threshold veya knockback aldı |
| Stunned | TakeDamage() sonrası 0.35s |

### Zorluk Seviyeleri
| | Kolay | Normal | Zor |
|-|-------|--------|-----|
| Reaction time | 1.2s | 0.7s | 0.22s |
| Parry şansı | %8 | %28 | %55 |
| Retreat HP | %40 | %25 | %10 |

### Hareket Sistemi
- Player-relative movement: ileri/geri = düşmana eksen, sağ/sol = Vector3.Cross (orbit yok)
- Auto-face: karakter her zaman düşmana dönük (lock-on açıkken)
- Geri yürüme: WalkSpeed = -1f → animasyon tersten oynar

### Kamera
- Lock-on açık: Player'ın arkasından SmoothDamp
- Lock-on kapalı: Sağ ekranda parmak sürükle = kamera döner, karakter kamerayla döner
- LOCK/FREE butonu — sağ orta ekran

### Ekran
- Landscape lock: Screen.orientation = LandscapeLeft
- Portrait tamamen kapalı

---

## 📋 Phase 4 — Karakter & İçerik (YAPILACAK)

### Asset Alımı (~$365)
- [ ] Gerçek karakter modelleri (en az 3-5 karakter)
- [ ] Her karaktere animasyon paketi
- [ ] Arena modelleri (en az 3 farklı harita)
- [ ] Crouch animasyonu (şu an yok)
- [ ] Skill animasyonları (SK1, SK2 placeholder)

### Karakter Sistemi
- [ ] Her karakterin farklı stats (speed, power, defense)
- [ ] Her karakterin 2 özel skill'i
- [ ] Karakter seçim ekranı
- [ ] Karakter kilitleme sistemi

---

## 📋 Phase 5 — Story/Turnuva Modu (YAPILACAK)

### Hikaye Konsepti (taslak)
- **Dünya:** İstanbul, yeraltı dövüş dünyası
- **Turnuva:** "VOLK" — kazananın bir dilek hakkı var, ne olduğu bilinmiyor
- **Ana karakter:** İstanbul sokaklarından çıkmış Türk savaşçı
- **Diğer karakterler:** Farklı şehir/kültürden, her birinin kendi sebebi var

### Gameplay Yapısı
- [ ] Sıralı düşmanlar sistemi (chapter bazlı)
- [ ] Her chapter'da boss
- [ ] Artan zorluk
- [ ] Karakter arası kısa diyalog/cinematic
- [ ] Oyuncu ilerleme kaydı (PlayerPrefs veya PlayFab)

---

## 📋 Phase 6 — Meta & Ekonomi (YAPILACAK)

- [ ] PlayFab backend entegrasyonu
- [ ] Combo unlock sistemi
- [ ] Shop UI
- [ ] Günlük görevler
- [ ] Sıralama tablosu

---

## 📋 Phase 7 — Multiplayer (YAPILACAK)

- [ ] Photon Quantum entegrasyonu (1v1 online)
- [ ] Lobi sistemi
- [ ] Eşleştirme (matchmaking)
- [ ] Lag compensation

---

## 📋 Phase 8 — Platform & Launch (YAPILACAK)

- [ ] Google Play Console hesabı ($25)
- [ ] APK → AAB build
- [ ] Oyun ikonu + splash screen
- [ ] Ekran görüntüleri (landscape)
- [ ] Privacy Policy
- [ ] İçerik derecelendirme formu
- [ ] Google Play yayını
- [ ] Apple Developer hesabı ($99/yıl)
- [ ] iOS Xcode build
- [ ] App Store yayını
- [ ] Domain (volkgame.com veya playvolk.com)
- [ ] Oyun fragmanı

---

## Bilinen Sorunlar / Pending

- [ ] Crouch animasyonu yok (BodyBlock kullanılıyor geçici)
- [ ] Double tap aksiyonları boş (SK1/SK2 gibi placeholder)
- [ ] Ses dosyaları eksik (sadece 1'er tane var, çoğaltılacak)
- [ ] iOS build test edilmedi
- [ ] Düşük RAM'li cihazlarda performans test edilmedi

---

## Yeni Bilgisayar Kurulum Sırası

```bash
# 1. Homebrew
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# 2. Araçlar
brew install git git-lfs node android-platform-tools

# 3. Unity Hub → unity.com/download
# Unity 2022.3.62f1 + Android Build Support modülü ekle

# 4. Repoyu çek
git clone https://github.com/MONOKRONE/volk.git
cd volk
git lfs install
git lfs pull

# 5. Claude Code
npm install -g @anthropic/claude-code
claude /login  # yeni hesapla giriş

# 6. Unity'de projeyi aç
# Unity Hub → Add → volk/Volk klasörünü seç

# 7. Android için adb test
adb devices
```
