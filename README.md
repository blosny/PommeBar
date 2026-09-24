# 🍎 PommeBar — Windows için Apple Music Widget'ı
### *A sleek, modern Windows 11 taskbar mini-player & widget for Apple Music.*

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows 11](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?logo=windows&logoColor=white)](https://microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent%20Design-0078D7)](https://github.com/lepoco/wpfui)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**PommeBar** (Fransızca elma anlamına gelen *'Pomme'* ve *'Taskbar'* birleşimi), Windows için Apple Music deneyimini masaüstünüze ve görev çubuğunuza taşıyan zarif, modern ve hafif bir mini oynatıcı widget'ıdır.

---

## ✨ Özellikler (Features)

- 🎵 **Gerçek Zamanlı Şarkı & Kapak Bilgisi:** Windows SMTC (*System Media Transport Controls*) entegrasyonu ile Apple Music'te çalan parçanın adını, sanatçısını ve albüm kapağını anlık olarak yansıtır.
- ⏯️ **Eksiksiz Medya Kontrolleri:** Oynat/Durdur, Sonraki Parça ve Önceki Parça butonları.
- ❤️ **Tek Tıkla Beğen (Love/Favorite):** Windows UI Automation altyapısını kullanarak arka plandaki Apple Music uygulamasını açmaya gerek kalmadan şarkıyı doğrudan favorilerinize ekler.
- 📍 **Akıllı Görev Çubuğu Konumlandırma:**
  - İlk açılışta veya sağ tık menüsünden **Sol (Başlat)**, **Orta (İkonlar)** veya **Sağ (Saat/Tepsi)** hizalaması seçilebilir.
  - İstenildiği takdirde fare ile ekranın herhangi bir yerine serbestçe sürüklenebilir.
- 🔊 **Fare Tekerleği ile Ses Kontrolü:** Widget üzerindeyken farenizin tekerleğini yukarı/aşağı kaydırarak sistem sesini anında ayarlayın.
- 🪟 **Hızlı Erişim:** Şarkı bilgisine veya albüm kapağına tıkladığınızda Apple Music penceresini otomatik olarak öne getirir.
- 🚀 **Windows ile Otomatik Başlatma:** Bilgisayar açıldığında widget'ın otomatik olarak görev çubuğunda hazır olmasını sağlar (sağ tık menüsünden açılıp kapatılabilir).
- 🎨 **Modern Windows 11 Tasarımı:** Yuvarlatılmış köşeler, koyu akrilik arka plan, yumuşak gölgeler ve Always-on-Top (her zaman üstte) modu.

---

## 📥 İndirme ve Çalıştırma (Download & Run)

PommeBar **Self-Contained (Bağımsız)** olarak derlenebilir; yani bilgisayarınızda .NET SDK veya Runtime kurulu olmasa bile çalışır!

1. [Releases](https://github.com/blosny/PommeBar/releases) sayfasından en son sürümü indirin.
2. `PommeBar.exe` dosyasına çift tıklayarak çalıştırın.
3. Açılan konum penceresinden görev çubuğu tercihinizi belirleyin ve müziğin tadını çıkarın!

---

## 🛠️ Kaynak Koddan Derleme (Building from Source)

Projeyi kendi ortamınızda geliştirmek veya derlemek için:

```bash
# Repoyu klonlayın
git clone https://github.com/blosny/PommeBar.git
cd PommeBar

# Projeyi derleyin
dotnet build

# Bağımsız (Self-contained) paket oluşturun
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

---

## 🎛️ Kullanım Kısayolları (Controls & Gestures)

| Eylem | İşlev |
|---|---|
| **Sol Tık + Sürükle** | Widget'ı ekranda istediğiniz yere taşıyın |
| **Şarkı Adına Tıklama** | Apple Music penceresini öne getirin |
| **Fare Tekerleği (Scroll)** | Sistem sesini kısıp açın |
| **Sağ Tık Menüsü** | Konumlandırma, Başlangıç ayarı ve Çıkış |
| **❤️ Kalp Butonu** | Çalan parçayı Apple Music favorilerine ekleyin |

---

## 🤝 Katkıda Bulunma (Contributing)

Katkılarınızı memnuniyetle bekliyoruz! Hata bildirimleri veya yeni özellik istekleri için lütfen [Issues](https://github.com/blosny/PommeBar/issues) açın veya bir Pull Request gönderin.

---

## 📄 Lisans (License)

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.
