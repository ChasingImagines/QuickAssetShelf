# Changelog

Tüm dikkate değer değişiklikler bu dosyada tutulur.
Biçim: [Keep a Changelog](https://keepachangelog.com/), sürümleme: [SemVer](https://semver.org/).

## [1.1.0] - 2026-09-12

### Added
- **Yoksayılan klasörler**: belirli klasörler (ve alt klasörleri) rafa hiç kaydedilmez, listede görünmez.
- Pencere içinde **🚫 Yoksayılan Klasörler** bölümü (seçili klasörü ekle / tek tek kaldır / tümünü temizle).
- Project paneli sağ tık menüsü: **Assets → Quick Asset Shelf → Yoksayılan Klasörlere Ekle / Çıkar**.

### Fixed
- Paket kök dosyaları (`README.md`, `LICENSE`, `CHANGELOG.md`, `package.json`) için `.meta`
  dosyaları eklendi; `has no meta file, but it's in an immutable folder` uyarıları kalktı.

## [1.0.0] - 2026-09-11

### Added
- Prefab ve ScriptableObject'ler için otomatik "son kullanılanlar" rafı (50 kayıt).
- Sabitleme (pin) desteği ve ayrı "Sabitlenenler" bölümü.
- İsimle arama + `Tümü / Prefab / SO` tip filtresi.
- Çift tıklama ile asset açma (Prefab Stage / Inspector).
- Damga (stamp) modu: Scene View'da `Shift + Sol Tık` veya `B` ile prefab yerleştirme.
- Ayar ve durum kalıcılığı (`EditorPrefs`); domain reload'da korunur.
- Editör-only `QuickAssetShelf.Editor` asmdef'i ve UPM `package.json`.
