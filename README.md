# 🛒 E-Commerce Sales Intelligence & Machine Learning Platform

Bu proje, e-ticaret platformlarındaki satış verilerini derinlemesine analiz eden, geleceğe yönelik satış tahminleri yürüten ve ML.NET kütüphanesini kullanarak yapay zeka destekli sınıflandırma ve anomali tespiti gerçekleştiren kapsamlı bir **ASP.NET Core MVC & Web API** projesidir.

---

## 🚀 Proje Hakkında ve Temel Amaç
E-ticaret operasyonlarında veriye dayalı kararlar alabilmek, stok yönetimi yapabilmek ve anormal durumları (ani satış düşüşleri/patlamaları) erken fark edebilmek hayati önem taşır. Bu platform;
* **Zaman Serisi Analizi** ile gelecek dönem satış tahminlemesi yapar,
* **Binary ve Multiclass Classification** ile ürün/satış performanslarını kategorize eder,
* **Anomaly Detection** ile olağan dışı günleri tespit eder,
* Performans optimizasyonu için **Streaming (Akışsal) Veri Yönetimi** kullanır.

---

## 🛠️ Kullanılan Teknolojiler ve Mimari

| Kategori | Kullanılan Teknolojiler / Araçlar |
| :--- | :--- |
| **Framework** | .NET (ASP.NET Core MVC & Web API) |
| **ORM / Veritabanı** | Entity Framework Core, **PostgreSQL / SQL Server**, LINQ |
| **Yapay Zeka / ML** | **ML.NET** (Microsoft Machine Learning Framework) |
| **Harici Kütüphaneler** | AutoMapper (Nesne Dönüşümleri) |
| **Mimari Yapı** | Service-Oriented Architecture (SOA), Controller-Service Pattern |

---

## 🧠 Yapay Zeka ve Makine Öğrenmesi Modelleri (ML.NET)

Proje içerisinde her biri farklı bir iş kuralını çözen **4 temel ML.NET servisi** bulunmaktadır:

### 1️⃣ Görev 1: Sales Forecasting (Gelecek Satış Tahmini)
* **Algoritma / Yaklaşım:** Zaman Serisi Analizi (`SsaForecastingEstimator`)
* **Açıklama:** Belirli bir şehrin geçmiş satış verilerini analiz ederek gelecek 7 günlük satış projeksiyonunu çıkarır.

### 2️⃣ Görev 2: Binary Classification (İkili Sınıflandırma ve Başarı Metrikleri)
* **Algoritma:** `SdcaLogisticRegression`
* **Açıklama:** Satışların başarısını tahmin eder. Model başarısını ölçmek için **Train/Test Ayrımı (%80 Eğitim - %20 Test)** uygulanır ve şu metrikler hesaplanır:
  * *Accuracy (Doğruluk)*
  * *F1 Score (F1 Skoru - Kesinlik ve Duyarlılığın Harmonik Ortalaması)*
  * *AUC - Area Under ROC Curve (ROC Eğrisi Altında Kalan Alan - Modelin Sınıflandırma Yeteneği)*
  * *Positive Precision & Recall (Pozitif Sınıf İçin Kesinlik ve Duyarlılık / Hatırlama)*

### 3️⃣ Görev 3: Multiclass Classification (Çoklu Sınıflandırma - Low/Medium/High)
* **Algoritma:** `SdcaMaximumEntropy`
* **Açıklama:** Ürünlerin satış miktarlarına (`Quantity`) dayanarak gelecek performanslarını **Low**, **Medium** veya **High** olmak üzere 3 farklı sınıfa ayırır.

### 4️⃣ Görev 4: Anomaly Detection (Anomali Tespiti)
* **Algoritma:** `IidSpikeEstimator` (Singular Spectrum Analysis tabanlı ani sıçrama tespiti)
* **Açıklama:** Günlük toplam satışları baz alarak normal davranışın dışına çıkan olağanüstü artışları veya çöküşleri tespit eder.


## 💡 Performans ve Bellek Optimizasyonları (Best Practices)

* **Streaming Veri Yönetimi (`IEnumerable` & `yield return`):** Büyük veri setleri veritabanından tek seferde çekilerek RAM şişirilmez; `AsNoTracking()` ile performans artışı sağlanıp veriler parça parça işlenir.
* **Bağımlılık Enjeksiyonu (DI):** Tüm servisler `Program.cs` üzerinde `AddScoped` ve `AddSingleton` (`MLContext` için) olarak mimari standartlara uygun biçimde kaydedilmiştir.

---

## 📂 Proje Katmanları ve Mimari Yapı (Solution Structure)

```text
ECommerceSalesIntelligence/
│
├── Context/
│   └── AppDbContext.cs                   # Veritabanı bağlantı sınıfı
│
├── Controllers/
│   ├── HomeController.cs                 # Ana sayfa/Arayüz kontrolcüsü
│   └── SalesIntelligenceController.cs    # ML servislerini yöneten API uç noktaları
│
├── Entities/
│   └── SalesRecord.cs                    # Veritabanı veri tablosu/modeli
│
├── Mappings/
│   └── GeneralMapping.cs                 # AutoMapper profilleri
│
├── Migrations/                           # Entity Framework veritabanı göçleri
│
├── Models/
│   ├── ErrorViewModel.cs                 # Hata yönetimi modeli
│   ├── SalesAnomalyInput.cs              # Anomali tespiti girdi modeli
│   ├── SalesAnomalyPrediction.cs         # Anomali tespiti çıktı modeli
│   ├── SalesAnomalyResultViewModel.cs    # Anomali arayüz görünüm modeli
│   ├── SalesClassificationInput.cs       # İkili sınıflandırma girdi modeli
│   ├── SalesClassificationPrediction.cs  # İkili sınıflandırma çıktı modeli
│   ├── SalesData.cs                      # Genel satış veri modeli
│   ├── SalesMulticlassInput.cs           # Çoklu sınıflandırma girdi modeli
│   ├── SalesMulticlassPrediction.cs      # Çoklu sınıflandırma çıktı modeli
│   └── SalesPrediction.cs                # Satış tahmin modeli
│
├── Services/
│   ├── AnomalyDetectionService.cs        # Anomali tespiti servisi
│   ├── ClassificationService.cs          # İkili sınıflandırma servisi
│   ├── ForecastingService.cs             # Zaman serisi tahmin servisi
│   └── MulticlassClassificationService.cs # Çoklu sınıflandırma servisi
│
├── Views/                                 # MVC Arayüz Görünümleri
├── appsettings.json                       # Konfigürasyon ve bağlantı cümlececikleri
└── Program.cs                             # Servis kayıtları (DI) ve Middleware ayarları
