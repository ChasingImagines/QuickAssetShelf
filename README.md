# Quick Asset Shelf

Unity Editörü için **Prefab ve ScriptableObject rafı**. Proje panelinde gezinmek yerine
sık kullandığın asset'leri tek pencereden; ara, sabitle, sürükle ve prefab'ları Scene View'a
tek tuşla yerleştir.

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black) ![License](https://img.shields.io/badge/license-MIT-blue)

---

## Özellikler

- **Otomatik kayıt** — Project panelinden bir Prefab veya ScriptableObject seçtiğinde rafa eklenir (son 50).
- **Sabitleme (📌)** — Önemli asset'ler "Sabitlenenler" bölümünde üstte kalır, her zaman erişilebilir.
- **Arama** — İsimle anında filtrele; `Tümü / Prefab / SO` tip filtresiyle birleşir.
- **Çift tıkla aç** — Prefab → Prefab Stage, ScriptableObject → Inspector.
- **Sürükle-bırak** — Rafdaki satırı Scene/Inspector'a sürükleyerek kullan.
- **Damga (Stamp) modu** — Bir prefab'ı işaretle, Scene View'da `Shift + Sol Tık` veya `B` ile yerleştir.
  Yüzey normaline göre yerleşir, `Undo` destekler.
- **Kalıcı** — Kayıtlar, sabitler ve aktif damga `EditorPrefs`'te tutulur; domain reload'da kaybolmaz.

---

## Kurulum

### Git URL ile (UPM)

**Window → Package Manager → + → Add package from git URL:**

```
https://github.com/ChasingImagines/QuickAssetShelf.git
```

veya `Packages/manifest.json` içine:

```json
"com.chasingimagines.quickassetshelf": "https://github.com/ChasingImagines/QuickAssetShelf.git#v1.0.0"
```

Sürüm sabitlemek için URL'nin sonuna `#v1.0.0` (veya bir commit hash'i) ekle. Sabitlemezsen
her zaman `main` dalının en son hali çekilir. Kurulum sırasında
`... has no meta file, but it's in an immutable folder` uyarıları çıkabilir; zararsızdır
(paket klasörü salt-okunurdur).

### Elle

`Editor/` klasörünü projenin `Assets/` altına kopyala. Başka bağımlılığı yok.

---

## Kullanım

1. **Tools → Quick Asset Shelf** ile pencereyi aç. Scene View yanına döşenir.
2. Project panelinden Prefab/SO seçtikçe rafa düşer (üstteki **● Dinliyor / ○ Duraklatıldı** ile kapatabilirsin).
3. **📍** ile sabitle, **✕** ile kaldır, isme tıklayarak asset'i ping'le, çift tıklayarak aç.
4. Bir prefab satırındaki **Damgala** ile damga moduna geç; Scene View'da `Shift + Sol Tık` veya `B`.

| Kısayol / Buton | İşlev |
|---|---|
| `Shift + Sol Tık` (Scene View) | Aktif damgayı yerleştir |
| `B` (Scene View) | Aktif damgayı yerleştir |
| `📍` | Sabitle / sabitlemeyi kaldır |
| Çift tık | Asset'i aç (Prefab Stage / Inspector) |

---

## Notlar

- Yalnızca **Prefab** ve **ScriptableObject** türleri rafa alınır.
- Geçmiş sınırı 50'dir (`QuickAssetShelfService.MaxHistory`).
- Tüm veriler `EditorPrefs` altında `QuickAssetShelf_*` anahtarlarında saklanır. **Temizle** düğmesi hepsini siler.

---

## Geliştirme

Paket tek dosyadır: `Editor/QuickAssetShelf.cs`. Editör-only bir asmdef (`QuickAssetShelf.Editor`)
ile derlenir, oyun build'ine dahil olmaz.

---

## Lisans

MIT — bkz. [LICENSE](LICENSE).
