# Changelog

Tüm dikkate değer değişiklikler bu dosyada tutulur.
Biçim: [Keep a Changelog](https://keepachangelog.com/), sürümleme: [SemVer](https://semver.org/).

## [1.0.0] - 2026-09-11

### Added
- Prefab ve ScriptableObject'ler için otomatik "son kullanılanlar" rafı (50 kayıt).
- Sabitleme (pin) desteği ve ayrı "Sabitlenenler" bölümü.
- İsimle arama + `Tümü / Prefab / SO` tip filtresi.
- Çift tıklama ile asset açma (Prefab Stage / Inspector).
- Damga (stamp) modu: Scene View'da `Shift + Sol Tık` veya `B` ile prefab yerleştirme.
- Ayar ve durum kalıcılığı (`EditorPrefs`); domain reload'da korunur.
- Editör-only `QuickAssetShelf.Editor` asmdef'i ve UPM `package.json`.
