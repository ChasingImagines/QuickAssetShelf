# Changelog

Tüm dikkate değer değişiklikler bu dosyada tutulur.
Biçim: [Keep a Changelog](https://keepachangelog.com/), sürümleme: [SemVer](https://semver.org/).

## [1.2.0] - 2026-09-13

### Added
- **Material desteği**: raf artık Prefab ve ScriptableObject'in yanında `Material` asset'lerini de kaydeder (rozet: `[Materyal]`).
- **Ayrı Ayarlar penceresi** (`Tools → Quick Asset Shelf → Ayarlar`): dinleme, izlenen türler,
  geçmiş sınırı, yoksayılan klasörler ve veri temizleme tek yerde toplandı. Ana penceredeki `⚙` butonuyla açılır.
- **Tür bazlı yoksayma**: izlenen türler (Prefab / ScriptableObject / Material) ayarlardan kapatılabilir;
  kapatılan türler rafa kaydedilmez ve listede görünmez.
- **Seçili türü temizle**: araç çubuğundaki `🗑` butonu, aktif filtrenin türündeki tüm kayıtları
  (sabitlenenler dahil) onay alarak kaldırır. Project asset'leri silinmez.
- **Geçmiş sınırı ayarı**: son kullanılanlar kayıt sayısı 10–200 arasında ayarlanabilir.
- Filtre seçeneklerine **Materyal** eklendi.

### Changed
- Ana pencere sadeleştirildi; ayar ve yoksayılan klasör yönetimi Ayarlar penceresine taşındı.
- Tüm türler kapalıyken ana pencere uyarı gösterir ve doğrudan Ayarlar'ı açan bir kısayol sunar.

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
