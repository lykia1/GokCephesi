# Gök Cephesi

İki ulusun (Arkon Federasyonu ve Velora Birliği) gökyüzünde savaştığı, AirRivals tarzı 3D mobil uçuş oyunu. Unity ile çalışır.
Model, sahne ya da prefab hazırlaman gerekmez: **Play'e bastığında oyun tüm haritaları, gemileri ve arayüzü kendisi kurar.**

---

## 1. Kurulum (ilk kez, ~20 dakika)

1. **Unity Hub**'ı indir: https://unity.com/download
2. Unity Hub → *Installs* → *Install Editor* → **Unity 6 (LTS)** seç.
   Modül ekranında **Android Build Support** kutusunu işaretle (içindeki OpenJDK ve SDK/NDK dahil). iPhone için Mac'te **iOS Build Support**.
3. Unity Hub → *Projects* → *New project* → **Universal 3D** şablonu → proje adı: `GokCephesi` → *Create*.
4. Bu zip'teki `Assets/GokCephesi` klasörünü, yeni projenin `Assets` klasörünün içine kopyala
   (Unity açıkken dosya gezgininden sürükleyip Project penceresindeki `Assets`'e bırakabilirsin).
5. Unity birkaç saniye derler. Alttaki *Console* penceresinde kırmızı hata olmamalı.
6. Üstteki ▶ **Play** butonuna bas. Oyun başlar.

> Bilgisayarda test: fare ile sol alt bölgeye tıklayıp sürükle (joystick), ekrandaki butonlara tıkla ya da klavyeyi kullan
> (WASD/oklar yön, Space ateş, E füze, Shift hız, Q fren, Tab hedef, Esc menü).
> Game penceresinin üstünden çözünürlüğü **1920x1080 (Landscape)** yapmanı öneririm.

## 2. Telefona yükleme (Android)

1. *File → Build Profiles* → **Android** → *Switch Platform*.
2. *Player Settings* → *Resolution and Presentation* → *Default Orientation*: **Auto Rotation**, sadece **Landscape Left** ve **Landscape Right** işaretli.
3. Telefonda *Geliştirici seçenekleri → USB hata ayıklama*'yı aç, USB ile bağla.
4. *Build And Run* → APK telefona kurulur ve açılır.
   (Ya da *Build* ile `.apk` üret, dosyayı telefona atıp kur.)

## 3. Oyunda neler var

| Özellik | Açıklama |
|---|---|
| **2 ulus** | Başta ulusunu seçersin (kalıcı). Karşı ulus düşmanındır. |
| **4 gemi sınıfı** | Avcı (hızlı), Saldırı (dengeli, güçlü füze), Zırhlı (dayanıklı), Bombardıman (kalelere 2,5 kat hasar). Hangar'dan ücretsiz değişir. |
| **4 harita + geçitler** | Ana Şehir (güvenli, hangar) → Kumul Vadisi (yaratık Sv.1-7) → Sınır Cephesi (Sv.4+, ulus savaşı) ve Kristal Kanyon (Sv.7+, yaratık Sv.7-16). Parlayan halkalardan geçerek geçiş yapılır. |
| **Seviye / XP** | Yaratık ve düşman pilot düşürerek XP ve kredi kazanırsın. Maks seviye 30. |
| **Gemi geliştirme** | Hangar'da Motor, Zırh, Silah, Kalkan; her biri 10 seviye. |
| **Ulus savaşı** | Sınır Cephesi'nde 3 üs (Alfa, Merkez, Delta) var. Üssün çemberine girip kalınca ele geçirilir. 3'ü de bir ulustaysa karşı kalenin kalkanı düşer; kaleyi yıkan ulus savaşı kazanır, büyük ödül alınır, savaş yeniden başlar. |
| **Yapay zekâ** | Dost ve düşman pilotlar üs ele geçirir, it dalaşı yapar, kaleye saldırır. Yaratıklar bölgesinde dolaşır, saldırınca karşılık verir. |
| **Kayıt** | İlerleme telefonda otomatik kaydedilir. Menü'den sıfırlanabilir. |

## 4. Kontroller (telefon)

- **Sol yarı:** dokun ve sürükle → gemiyi yönlendir (yukarı = burun yukarı; Menü'den ters çevrilebilir).
- **ATEŞ:** basılı tut. Lazer kilitli hedefe otomatik yönelir.
- **FÜZE:** hedefe güdümlü füze, bekleme süresi butonda görünür.
- **HIZ / FREN:** hızlan, yavaşla.
- **HEDEF:** sıradaki düşmana kilitlen. (Önündeki düşmana otomatik kilitlenir.)
- Üst sağ: **radar** (yeşil dost, kırmızı düşman, sarı yaratık, mavi geçit). Yanında **MENÜ**, **HANGAR** / **ŞEHRE DÖN**.

## 5. Dosyalar

| Dosya | Görevi |
|---|---|
| `Bootstrap.cs` | Play'e basınca oyunu otomatik başlatır |
| `GameData.cs` | Uluslar, gemi sınıfları, haritalar, seviye formülleri, kayıt — **denge ayarları burada** |
| `GameManager.cs` | Ekran akışı, harita geçişi, ödüller, ulus savaşı |
| `World.cs` | Haritaları kodla kurar (zemin, binalar, dağlar, kristaller) |
| `Structures.cs` | Geçitler, üsler, kaleler |
| `Ship.cs` | Uçuş, silahlar, hasar, gemi görünümleri |
| `BotPilot.cs` | Yapay zekâ pilotlar ve yaratıklar |
| `PlayerPilot.cs` | Oyuncu kontrolü ve hedef kilitleme |
| `Projectile.cs` | Lazer ve füzeler |
| `MobileControls.cs`, `InputBridge.cs` | Dokunmatik joystick/butonlar, klavye, fare |
| `HUD.cs` | Tüm arayüz: seçim ekranları, göstergeler, hangar, menü |
| `ChaseCamera.cs`, `Gfx.cs` | Kamera; malzemeler, efektler, kodla üretilen sesler |

Kolay değişiklik örnekleri (`GameData.cs`):
- Ulus adları/renkleri: `Nations.Names`, `Nations.Main`
- Gemi gücü: `ShipClasses.All` içindeki `hp`, `speed`, `laserDmg`...
- Harita zorluğu: `Maps` içinde `monsterCount`, `minMonsterLv`, `enemyBots`, `minLevel`

## 6. Sorun giderme

- **Her şey pembe görünüyor:** Proje URP ise *Edit → Project Settings → Graphics*'te bir URP Asset atanmış olmalı (Universal 3D şablonunda hazır gelir).
- **Console'da kırmızı hata:** Hatanın metnini kopyalayıp bana gönder.
- **Telefonda dikey açılıyor:** 2. bölümdeki yön ayarını yap.
