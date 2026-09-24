# PommeBar - Windows için Apple Music Widget'ı

Harika bir fikir! Açık kaynak (Open Source) olarak [PommeBar GitHub](https://github.com/blosny/PommeBar) reposunda yayınlamak projeyi çok daha değerli kılacaktır. 

PommeBar (Fransızca elma anlamına gelen 'Pomme' ve Taskbar birleşimi), Apple Music'e ince bir gönderme yaparak şık ve modern bir Windows görev çubuğu widget'ı olmayı hedefliyor.

---

## 🎯 Özellikler (Features)

Projeyi iki aşamaya bölmek GitHub'da daha hızlı bir ilk sürüm (v1.0) yayınlamamızı sağlar.

### Sürüm 1.0 (İlk Hedeflerimiz - MVP)
- [ ] **Şarkı Bilgisi:** Çalan şarkının adı, sanatçısı ve albüm kapağını (küçük ikon olarak) gösterme.
- [ ] **Temel Kontroller:** Oynat/Durdur, Önceki Şarkı, Sonraki Şarkı tuşları (Windows SMTC ile entegre, çok stabil).
- [ ] **Beğen (Love) Butonu:** Yeni Windows "Apple Music" uygulamasına otomasyon ile bağlanıp şarkıyı favorilere ekleme tuşu.
- [ ] **Otomatik Başlatma:** Bilgisayar açıldığında widget'ın otomatik çalışması.
- [ ] **Modern Tasarım:** Windows 11 tasarım diline uygun (Mica materyali, yuvarlak köşeler, karanlık/aydınlık mod desteği) borderless (çerçevesiz) mini bir pencere.

### Sürüm 2.0 (Gelecek Planları)
- [ ] **Şarkı Sözleri (Lyrics):** Tıklayınca şarkı sözlerini gösteren mini bir popup.
- [ ] **Ses Kontrolü:** Sadece müziğin sesini kısıp açabilen bir kaydırıcı (slider).
- [ ] **"Dislike" Butonu:** Şarkıyı daha az öner seçeneği.

---

## 🛠 Teknik Altyapı ve Kararlar

- **Proje İsmi:** PommeBar
- **GitHub Repo:** https://github.com/blosny/PommeBar
- **Hedef Klasör:** `c:\projects\pomme-bar`
- **Hedef Müzik Uygulaması:** Microsoft Store'daki yeni "Apple Music" uygulaması.
- **Teknoloji:** C# WPF (.NET 8) ile Windows 11 tasarım standartları.

---

## 📋 Görev Listesi (Tasks)

- [/] **Adım 0: Git ve Proje Kurulumu**
  - `c:\projects\pomme-bar` klasöründe çalışılması.
  - C# WPF proje iskeletinin kurulması (.NET 8).
  - Git repository'sinin başlatılması ve `https://github.com/blosny/PommeBar.git` adresine bağlanması.
- [ ] **Adım 1: Temel Arayüz (UI) ve Branch Yönetimi**
  - `feature/ui-design` branch'inin açılması.
  - Görev çubuğuna benzeyen, ekran görüntüsündeki gibi genişleyebilen modern (Mica efektli) karanlık tema arayüzün (XAML) tasarlanması.
  - `main` branch'ine merge (birleştirme) yapılması.
- [ ] **Adım 2: Medya Kontrolleri (SMTC Entegrasyonu)**
  - `feature/media-controls` branch'inin açılması.
  - Windows'ta çalan şarkının (Apple Music) adını, sanatçısını ve albüm kapağını çekme.
  - Oynat/Durdur, İleri/Geri butonlarının çalışır hale getirilmesi.
  - `main` branch'ine merge edilmesi.
- [ ] **Adım 3: 'Beğen' Butonu (UI Automation)**
  - `feature/like-button` branch'inin açılması.
  - Yeni Apple Music uygulamasının penceresini arka planda bulup "Beğen" tuşuna basacak otomasyon kodunun yazılması.
  - `main` branch'ine merge edilmesi.
- [ ] **Adım 4: Otomatik Başlatma ve Son Ayarlar**
  - `feature/startup` branch'inin açılması.
  - Windows başlangıcına ekleme özelliğinin kodlanması.
  - `main` branch'ine merge edilmesi.

> [!NOTE]
> Proje altyapısı bu plana göre kurulacaktır. Kuruluma ve kodlamaya başlamak için lütfen onay verin.
