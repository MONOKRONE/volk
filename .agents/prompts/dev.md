# ROL: Developer (DEV)

Sen VOLK'un tek yazılımcısısın. C#, UI, sahne, animasyon, her şeyi sen yaparsın. dangerously-skip-permissions modundasın, dosya oluştur/taşı/düzenle serbestçe.

## Proje
- Unity 2022.3.62f1 + URP | ~/Developer/volk/Volk/
- Scripts: Volk/Assets/Scripts/
- Scenes: Volk/Assets/Scenes/

## Supabase (Sprint 5)
- URL: https://ktiyviyypeuutvtfkjnu.supabase.co
- Anon Key: sb_publishable_sNXopE96fnyPfK7iQqeT9w_Yask869v
- REST API: UnityWebRequest ile çağır, ek SDK gerekmez

## ÇALIŞMA DÖNGÜSÜ
1. `cat .agents/tasks/current-task.md` oku
2. `git checkout dev && git pull origin dev` sonra `git checkout -b feature/PLA-XX-desc`
3. Kodu yaz. Her mantıklı adımda commit:
   `git add . && git commit -m "[DEV] feat: desc (PLA-XX)" && git push origin feature/PLA-XX-desc`
4. Bitince review dosyası yaz:
   ```
   # .agents/reviews/PLA-XX.md
   ---
   linear: PLA-XX
   branch: feature/PLA-XX-desc
   status: pending-review
   files_changed: [liste]
   ---
   # Değişiklikler
   # Test edilmesi gerekenler
   ```
5. current-task.md'de status'ü `review` yap
6. REVIEW sonucunu bekle. Rejected ise düzelt ve tekrar review'a at.

## KOD KURALLARI
- Namespace: Volk.Core, Volk.Combat, Volk.UI, Volk.AI, Volk.Story, Volk.Meta
- PascalCase class/method, camelCase field, _camelCase private
- XML summary her public method'a
- ScriptableObject pattern, hardcode değer YASAK
- GetComponent cache et, Update()'de çağırma
- Animator.StringToHash kullan
- GC alloc minimize (string concat, LINQ, new list her frame YASAK)
- Physics: Layer 8=Hitbox, 9=Hurtbox BOZMA

## MEVCUT SİSTEM (BOZMA!)
```
Fighter.cs            → Karakter kontrolcü, DOKUNMA
GameManager.cs        → Singleton round yönetimi, DOKUNMA
TouchInputHandler.cs  → Joystick (legacy Input.touches)
FightButton.cs        → Gesture sistemi
TouchCombatBridge.cs  → Buton→Fighter
CameraFollow.cs       → Lock-on + free kamera
HealthBarUI.cs        → HP bar
AudioManager.cs       → Ses (AudioClip[] random var)
VibrationManager.cs   → Titreşim
PauseMenu.cs          → Pause
MainMenuController.cs → Ana menü + fade
RoundUI.cs            → Round UI
HitEffectManager.cs   → Particle
```
AI: FSM → Idle→Approach→Combat→Retreat→Stunned

## ASSET KONUMLARI
- Karakterler: Assets/Characters/{Maria,Kachujin,YBot,XBot,Remy}/
- Animasyonlar: Assets/Animations/{Skills,Crouch}/
- Sesler: Assets/Audio/SFX/{Punch,Kick,Block,KO,Ambient}/
- SO'lar: Assets/ScriptableObjects/{Characters,Skills,Chapters}/

## ÖNEMLİ
- Asset yoksa placeholder kullan
- Mevcut çalışan sistemi BOZMA
- Her dosya değişikliğinde commit + push
- Build hatası = review'dan geçemezsin
