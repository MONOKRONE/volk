---
linear: PLA-44
status: pending
branch: feature/PLA-44-asset-organize
---
# [Sprint 0] Asset'leri Downloads'tan projeye yerleştir

## Ne yapilacak

Kullanici asset'leri ~/Downloads/ altina indirmis olacak. DEV bunlari Unity proje yapisina yerlestirecek.

## Klasor yapisini olustur

```bash
mkdir -p ~/Developer/volk/Volk/Assets/Characters/{Maria,Kachujin,YBot,XBot,Remy}
mkdir -p ~/Developer/volk/Volk/Assets/Animations/{Skills,Crouch}
mkdir -p ~/Developer/volk/Volk/Assets/Audio/SFX/{Punch,Kick,Block,KO,Ambient}
mkdir -p ~/Developer/volk/Volk/Assets/ScriptableObjects/{Characters,Skills,Chapters}
```

## Asset'leri Downloads'tan tasi

DEV: ~/Downloads/ icindeki tum ilgili dosyalari tara ve tasi:

- FBX karakter dosyalari -> Assets/Characters/[Isim]/
- FBX animasyon dosyalari -> Assets/Animations/Skills/ veya Crouch/
- WAV/OGG ses dosyalari -> Assets/Audio/SFX/[Kategori]/
- Dosya isimlerine gore akilli eslestirme yap

## Sonra yap

1. Her FBX'in Humanoid rig olarak import ayarini kontrol et
2. Animasyon FBX'lerin "Without Skin" import ayarini kontrol et
3. Ses dosyalarini Unity'de: mono, 22050Hz, Vorbis compress
4. git add + commit + push

## Kabul Kriterleri

- [ ] Tum karakter FBX'ler Assets/Characters/ altinda
- [ ] Tum animasyon FBX'ler Assets/Animations/ altinda
- [ ] Tum ses dosyalari Assets/Audio/SFX/ altinda kategorize
- [ ] Git commit yapildi

## Branch: `feature/PLA-44-asset-organize`
