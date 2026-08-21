# 🛒 E-Commerce Sales Intelligence & Machine Learning Platform

Bu proje, e-ticaret platformlarındaki satış verilerini analiz eden, geçmiş satış davranışlarından anlamlı sonuçlar çıkaran, geleceğe yönelik satış tahminleri yapan ve **ML.NET** kullanarak makine öğrenmesi modelleriyle sınıflandırma, kümeleme ve anomali tespiti gerçekleştiren kapsamlı bir **ASP.NET Core MVC & Web API** projesidir.


## 🚀 Proje Hakkında ve Temel Amaç

E-ticaret operasyonlarında satış verilerinin yalnızca listelenmesi yerine, bu verilerin analiz edilerek **karar destek mekanizmalarına dönüştürülmesi** amaçlanmıştır.
Platform;

* 📊 Satışların genel durumunu analiz eder,
* 📈 Geçmiş verilere göre gelecek satışları tahmin eder,
* 🎯 Satış performanslarını sınıflandırır,
* 🚨 Olağan dışı satış hareketlerini tespit eder,
* 🏙️ Şehirlerin satış davranışlarını kümelere ayırır,
* 🧠 ML.NET modelleriyle verilerden öğrenilebilir sonuçlar üretir,
* 📋 Model sonuçlarını kullanıcıya anlaşılır dashboard ekranları üzerinden sunar.

Proje içerisinde **Dashboard, Forecasting, Binary Classification, Multiclass Classification, Anomaly Detection ve Clustering** olmak üzere farklı analiz ve makine öğrenmesi ekranları bulunmaktadır.

---
## 🛠️ Kullanılan Teknolojiler ve Mimari

| Kategori | Kullanılan Teknolojiler / Araçlar |
| :--- | :--- |
| **Framework** | .NET / ASP.NET Core MVC & Web API |
| **ORM / Veritabanı** | Entity Framework Core / SQL Server, LINQ |
| **Yapay Zeka / ML** | ML.NET |
| **Frontend** | Razor View, HTML, Tailwind CSS, JavaScript |
| **Mimari Yapı** | Controller-Service Pattern, Service-Oriented Architecture |
| **Veri Erişimi** | Entity Framework Core, `AsNoTracking()` |
| **Makine Öğrenmesi** | Forecasting, Binary Classification, Multiclass Classification, Anomaly Detection, Clustering |

---
# 📊 Sayfalar ve İşlevleri
## 1️⃣ Dashboard — Genel Satış Analiz Paneli
### 🎯 Amaç
Dashboard sayfasının amacı, veritabanındaki tüm satış verilerini tek bir ekranda **genel ve karşılaştırılabilir satış göstergelerine dönüştürerek** kullanıcının işletmenin mevcut durumunu hızlı bir şekilde analiz edebilmesini sağlamaktır.

Dashboard üzerinde yalnızca toplam satış bilgileri değil, satışların **zaman, kategori, şehir, ödeme yöntemi, kampanya ve ürün bazındaki dağılımları** da gösterilir.

### 📌 Dashboard'da Sunulan Bilgiler
#### 💰 Genel Satış KPI'ları

Sistemdeki tüm satış kayıtları üzerinden:
* **Toplam Gelir**
* **Toplam Satılan Ürün Miktarı**
* **Toplam Satış Kaydı**
* **Ortalama Satış Tutarı**
* **Ortalama Birim Fiyat**
* **Kampanyalı Satış Oranı**
* **Satışların Başlangıç Tarihi**
* **Satışların Bitiş Tarihi**
hesaplanır.

Bu değerler sayesinde kullanıcının satış verisinin genel büyüklüğünü ve performansını tek bakışta değerlendirmesi sağlanır.

### 📅 Günlük Satış Analizi 

Satış kayıtları `OrderDate` alanına göre gün bazında gruplanır.
Her gün için:
* Toplam gelir,
* Toplam satılan ürün miktarı

hesaplanarak **günlük satış trendi** oluşturulur.
Bu bölüm, satışların zaman içerisindeki değişimini ve dönemsel hareketlerini gözlemlemek için kullanılır.

---

### 🏷️ Kategori Bazlı Satış Analizi
Satışlar `CategoryName` alanına göre gruplanır.
Her kategori için:
* Toplam gelir,
* Toplam satış miktarı,
* Toplam gelir içerisindeki yüzde payı
hesaplanır.

Böylece hangi ürün kategorilerinin işletmenin toplam gelirine daha fazla katkı sağladığı analiz edilir.

### 🏙️ Şehir Bazlı Satış Analizi

Satışlar şehirlere göre gruplanarak şehir performansları karşılaştırılır.
Her şehir için:
* Toplam gelir,
* Toplam satılan ürün miktarı,
* Ortalama satış tutarı

hesaplanır.
Dashboard üzerinde gelir açısından **en yüksek performans gösteren 10 şehir** listelenir.
Bu analiz özellikle ilerleyen aşamada kullanılan **Clustering** modeli için satış davranışlarının anlaşılmasına temel oluşturur.

---

### 💳 Ödeme Yöntemi Analizi

Satışlar kullanılan ödeme yöntemine göre gruplanır.
Her ödeme yöntemi için:
* Toplam gelir,
* Toplam satış miktarı,
* Toplam gelir içerisindeki yüzde payı

hesaplanır.

Bu sayede müşterilerin hangi ödeme yöntemlerini daha fazla kullandığı ve hangi yöntemlerin toplam satış gelirine daha fazla katkı sağladığı görülebilir.


### 🎯 Kampanya Satış Analizi

Satış kayıtları `IsCampaign` değerine göre iki gruba ayrılır:
* Kampanyalı satışlar
* Kampanyasız satışlar

Her iki grup için:
* Gelir,
* Satılan ürün miktarı,
* Satış kayıt sayısı,
* Toplam satış kayıtları içerisindeki oran

hesaplanır.

Bu bölüm sayesinde kampanyaların satış hacmi ve gelir üzerindeki etkisi karşılaştırılabilir.

### 🏆 En Çok Satan Ürünler

Ürünler `ProductName` alanına göre gruplanır.
Her ürün için:
* Toplam satış miktarı,
* Toplam gelir

hesaplanır.

Satış miktarı en yüksek olan **ilk 10 ürün** Dashboard üzerinde gösterilir.
Bu bölüm, işletmenin en çok talep gören ürünlerini belirlemek ve ürün performansını hızlı şekilde değerlendirmek için kullanılır.


### ⚙️ Dashboard Veri İşleme Yaklaşımı
Dashboard verileri doğrudan veritabanından alınarak `DashboardService` içerisinde analiz edilir.

Veri sorgularında:
* `Entity Framework Core`
* `LINQ`
* `AsNoTracking()`
* `GroupBy`
* `Sum`
* `Average`
* `LongCount`
* `OrderByDescending`
* `Take`

gibi yapılar kullanılarak satış verileri farklı analiz gruplarına ayrılır.
Elde edilen sonuçlar `DashboardViewModel` içerisinde toplanarak MVC View katmanına gönderilir.
Bu yapı sayesinde Controller yalnızca isteği karşılayıp servisi çağırırken, **satış analizlerinin hesaplanması Service katmanında gerçekleştirilir.**

<img width="1919" height="908" alt="Ekran görüntüsü 2026-08-21 182412" src="https://github.com/user-attachments/assets/a59dd6a6-c56e-4f78-8ad5-5282fcd26c9f" />
<img width="1917" height="580" alt="Ekran görüntüsü 2026-08-21 182432" src="https://github.com/user-attachments/assets/73cc7ccd-0d00-408e-b0c8-010b94baede2" />


---
# 🧠 Yapay Zeka ve Makine Öğrenmesi Modelleri (ML.NET)
Proje içerisinde farklı iş problemlerini çözmek amacıyla aşağıdaki makine öğrenmesi yaklaşımları kullanılmaktadır:

## 2️⃣ 📈 Satış Tahmini (Forecasting)

**Amaç:**  
Forecasting sayfasının amacı, geçmiş satış verilerini kullanarak belirli bir şehir için gelecekte gerçekleşmesi beklenen satış miktarlarını tahmin etmektir. Böylece işletmenin gelecekteki satış eğilimini görmesi, stok ve satış planlaması yapabilmesi ve beklenmeyen satış hareketlerini daha kolay değerlendirebilmesi sağlanır.

### 🔹 Sayfa Nasıl Çalışıyor?
Kullanıcı Forecasting sayfasında bir **şehir** seçerek o şehre ait satış geçmişi üzerinden tahmin oluşturabilir.
Sistem şu adımları izler:

1. 🏙️ Seçilen şehir için veritabanındaki satış kayıtları alınır.
2. 📅 Satış kayıtları **günlük toplam satış miktarına** dönüştürülür.
3. 🔄 Satış kaydı bulunmayan günler **0 satış** olarak tamamlanır.
4. 📊 Böylece model için kesintisiz bir günlük zaman serisi oluşturulur.
5. ✅ Tahmin yapılabilmesi için minimum veri kontrolleri gerçekleştirilir.
6. 🤖 ML.NET kullanılarak **SSA (Singular Spectrum Analysis)** tabanlı zaman serisi modeli oluşturulur.
7. 🧠 Model geçmiş satış davranışlarını analiz ederek gelecekteki satış miktarlarını tahmin eder.
8. 🔮 Varsayılan olarak **gelecek 7 günlük satış tahmini** oluşturulur.
9. 📈 Her tahmin günü için beklenen satış miktarı hesaplanır.
10. ⬇️⬆️ Tahminin **alt ve üst güven sınırları** oluşturulur.
11. 📊 Son 30 günlük gerçek satış verisi tahmin sonuçlarıyla birlikte gösterilmek üzere hazırlanır.

### 🧠 Kullanılan Makine Öğrenmesi Yöntemi
Forecasting işlemi için **ML.NET `ForecastBySsa`** yöntemi kullanılmaktadır.
SSA, geçmişteki zaman serisi verilerindeki tekrar eden davranışları ve eğilimleri analiz ederek gelecekteki değerleri tahmin etmek için kullanılan bir zaman serisi yaklaşımıdır.

<img width="1920" height="1697" alt="localhost_7189_Forecasting_Index (1)" src="https://github.com/user-attachments/assets/58b32451-7e5f-4d99-ae43-b0b0f67956bc" />

 
## 3️⃣ 📊 Satış Performansı Sınıflandırması (Binary Classification)
**Amaç:**  
Binary Classification sayfasının amacı, geçmiş satış verilerini kullanarak **şehir-ürün bazında gelecek ay satış performansının belirlenen eşik değerini aşıp aşmayacağını** tahmin etmektir.
Bu sayede hangi şehir ve ürünlerin gelecek ay **yüksek satış performansı gösterebileceği** önceden tahmin edilerek satış ve stok planlamasına destek olunur.

### 🔹 Sayfa Nasıl Çalışıyor?

Sistem satış verilerini **şehir + ürün + ay** bazında analiz ederek aşağıdaki süreci uygular:
1. 🗄️ Veritabanındaki satış kayıtları alınır ve gerekli alanlar seçilir.
2. 📅 Satışlar **şehir, ürün ve ay** bazında gruplanarak aylık satış miktarları hesaplanır.
3. 📊 En az **4 aylık satış geçmişine** sahip şehir-ürün grupları modele dahil edilir.
4. 🔄 Geçmiş satışlardan modelin kullanacağı özellikler oluşturulur:
   - Son 3 aylık toplam satış
   - Son ay satış miktarı
   - Son 3 aylık ortalama satış
5. 🎯 Gerçek hedef satış miktarı **650 ve üzerindeyse `EVET`**, 650'nin altındaysa **`HAYIR`** sınıfı oluşturulur.
6. 🧪 Veri seti **%80 eğitim ve %20 test** olarak ayrılır.
7. 🔤 Şehir ve ürün bilgileri **One-Hot Encoding** yöntemiyle sayısal verilere dönüştürülür.
8. 📐 Sayısal özellikler **Min-Max Normalization** ile ölçeklendirilir.
9. 🤖 **ML.NET `SdcaLogisticRegression`** algoritması kullanılarak model eğitilir.
10. 📈 Model test verileri üzerinde değerlendirilerek başarı metrikleri hesaplanır.
11. 🔮 Eğitilen model kullanılarak mevcut şehir-ürün gruplarının **gelecek ay satış performansı** tahmin edilir.
12. 📊 Her tahmin için sınıf, olasılık ve skor değerleri oluşturulur.
13. 🥇 Tahmin sonuçları en yüksek pozitif sınıf olasılığına göre sıralanır.
14. ⚡ Dashboard sonucu **1 saat boyunca MemoryCache** içerisinde tutulur. Böylece model her sayfa açılışında tekrar eğitilmez.

### 🎯 Sınıflandırma Mantığı

Modelde kullanılan satış eşiği **650 adet** olarak belirlenmiştir.

    Satış Miktarı ≥ 650
            ↓
          EVET
            ↓
    Yüksek satış performansı

    Satış Miktarı < 650
            ↓
          HAYIR
            ↓
    Düşük satış performansı

Bu eşik kod içerisinde `ClassificationThreshold = 650f` olarak tanımlanmıştır.

<img width="1920" height="1853" alt="localhost_7189_BinaryClassification_Index_page=7" src="https://github.com/user-attachments/assets/c31474f1-551d-4305-8fb5-8a883d420107" />


## 4️⃣ 📊 Multiclass Classification — Çoklu Sınıflandırma

**Amaç:**  
Multiclass Classification sayfasının amacı, şehir ve ürün bazındaki geçmiş satış davranışlarını analiz ederek **gelecek ayın satış performansını** tahmin etmektir.

Sistem her şehir-ürün kombinasyonunu üç farklı performans sınıfından birine ayırır:

- 🟢 **Low — Düşük Talep**
- 🟡 **Medium — Orta Talep**
- 🔴 **High — Yüksek Talep**

### 🔹 Sayfa Nasıl Çalışıyor?

Sistem aşağıdaki adımları izler:

1. 🗄️ Veritabanındaki satış kayıtları alınır.
2. 🏙️ Satışlar **şehir + ürün + ay** bazında gruplanır.
3. 📅 Her şehir-ürün kombinasyonu için aylık toplam satış miktarı hesaplanır.
4. 📊 En az **4 aylık satış geçmişine** sahip şehir-ürün grupları kullanılır.
5. 📈 Geçmiş üç ayın satış değerleri kullanılarak model özellikleri oluşturulur.
6. 🧮 Son üç ayın **ortalama satış miktarı** hesaplanır.
7. 📈 Son aylardaki değişimi göstermek için **büyüme oranları** hesaplanır.
8. 📐 Son üç aylık satışlardan **trend eğimi (Trend Slope)** hesaplanır.
9. 🎯 Gerçek hedef ay satışının, son üç aylık ortalamaya oranı hesaplanır.
10. 📌 Performans oranları sıralanarak **P33 ve P66 eşikleri** belirlenir.
11. 🏷️ Eğitim verileri **Low, Medium ve High** sınıflarına ayrılır.
12. ✂️ Veriler **%80 eğitim / %20 test** olarak bölünür.
13. 🤖 ML.NET ile **LbfgsMaximumEntropy** algoritması kullanılarak model eğitilir.
14. 📊 Test verileri üzerinden model performansı ölçülür.
15. 🔮 Her şehir-ürün kombinasyonu için gelecek ay tahmini oluşturulur.
16. 🎯 Tahmin sonucu **Low, Medium veya High** olarak belirlenir.
17. 📈 Tahminin yaklaşık güven değeri hesaplanarak dashboard'da gösterilir.


### 🧠 Kullanılan Makine Öğrenmesi Yöntemi

Multiclass Classification işleminde ML.NET'in **`LbfgsMaximumEntropy`** algoritması kullanılmaktadır.
Şehir ve ürün gibi kategorik veriler **One-Hot Encoding** yöntemiyle sayısal özelliklere dönüştürülür.
Satış miktarı, büyüme oranları ve trend gibi sayısal özellikler ise **Min-Max Normalization** işleminden geçirilerek modele aktarılır.
Modelin kullandığı temel özellikler:

- 🏙️ Şehir
- 📦 Ürün
- 📊 3 ay önceki satış
- 📊 2 ay önceki satış
- 📊 Son ay satış
- 📈 3 aylık satış ortalaması
- 📈 Son ay büyüme oranı
- 📈 2 aylık büyüme oranı
- 📊 Son ayın 3 aylık ortalamaya oranı
- 📐 Trend eğimi
- 📅 Tahmin edilecek ayın numarası

---

### 🏷️ Low / Medium / High Sınıflarının Oluşturulması

Sınıflar sabit bir satış miktarına göre belirlenmez.

Bunun yerine hedef ayın gerçek satış miktarının geçmiş üç aylık ortalamaya oranı kullanılır:

**Hedef Ay Satışı**  
↓  
**Son 3 Ay Ortalama Satışı**  
↓  
**Performans Oranı**  
↓  
**P33 ve P66 Eşikleri**  
↓

| Performans Oranı | Sınıf | Talep Seviyesi |
| :--- | :--- | :--- |
| Performans < P33 | 🔴 **LOW** | Düşük Talep |
| P33 ≤ Performans < P66 | 🟡 **MEDIUM** | Orta Talep |
| Performans ≥ P66 | 🟢 **HIGH** | Yüksek Talep |

Bu yöntem sayesinde satış performansı **veri dağılımına göre dinamik olarak** Low, Medium ve High sınıflarına ayrılır.

<img width="1920" height="1415" alt="localhost_7189_MulticlassClassification_Index" src="https://github.com/user-attachments/assets/173737c8-49f1-400a-bece-3a4df1de2e68" />

## 5️⃣ Görev 5: Anomaly Detection — Anomali Tespiti

* **Algoritma:** `IidSpikeEstimator` / ML.NET SSA Spike Detection
* **Amaç:** Günlük satış davranışındaki normalden sapmaları tespit etmek.

Model;

* 📈 Ani satış artışlarını,
* 📉 Ani satış düşüşlerini,
* ⚠️ Normal davranıştan önemli ölçüde ayrılan günleri
belirlemek için kullanılır.

### 🔍 Anomali Tespit Süreci
Sistem öncelikle veritabanındaki satış kayıtlarını **ülke, şehir, ürün ve gün** bazında gruplandırır.
Daha sonra her şehir-ürün serisi ayrı ayrı analiz edilir.

```text
Veritabanındaki Satış Kayıtları
            ↓
Ülke + Şehir + Ürün Gruplandırması
            ↓
Günlük Toplam Satışların Hesaplanması
            ↓
Eksik Günlerin 0 Satış ile Tamamlanması
            ↓
Minimum Veri Kontrolü
            ↓
ML.NET SSA Spike Detection
            ↓
Anomali Günlerinin Belirlenmesi
            ↓
Beklenen Satışın Hesaplanması
            ↓
Gerçek Satış ↔ Beklenen Satış Karşılaştırması
            ↓
Sapma Yüzdesinin Hesaplanması
            ↓
Anomali Durumu + Şiddeti
            ↓
Dashboard Sonuçları
```

### 📊 Kullanılan SSA Parametreleri

Anomali tespitinde aşağıdaki parametreler kullanılmaktadır:
| Parametre | Değer | Açıklama |
|---|---:|---|
| `TrainingWindowSize` | `60` | Modelin geçmiş 60 günlük veriyi analiz etmesini sağlar. |
| `SeasonalityWindowSize` | `7` | Haftalık satış davranışının dikkate alınmasını sağlar. |
| `Confidence` | `95` | Anomali tespitinde %95 güven seviyesi kullanılır. |
| `PValueHistoryLength` | `30` | Son 30 gözlem üzerinden p-value geçmişi değerlendirilir. |
| `MinimumSeriesLength` | `60` | Bir serinin analiz edilebilmesi için en az 60 günlük veri gerekir. |
| `MinimumSalesDays` | `30` | En az 30 gerçek satış günü bulunması gerekir. |

### 🧠 Eksik Günlerin İşlenmesi
Satış kaydı bulunmayan tarihler zaman serisinin kesintisiz olması için `0` satış olarak tamamlanır.
Ancak beklenen satış hesaplanırken **0 satış olan günler ortalamaya dahil edilmez**.
Böylece eksik kayıtlar modelin zaman serisini bozmazken, beklenen satış değeri de gereksiz şekilde düşürülmez.

### 🎯 Beklenen Satışın Hesaplanması
Anomali tespit edilen günün normalde kaç satış yapmasının beklendiğini belirlemek için üç aşamalı bir yaklaşım kullanılır.

Yeterli aynı hafta günü bulunamazsa:
```text
2. Son 14 gün
       ↓
En az 3 gerçek satış
       ↓
MEDYAN
```

### 📐 Median Kullanımının Nedeni
Beklenen satış hesaplamasında ortalama yerine **median (medyan)** kullanılır.
Bunun nedeni, geçmişte gerçekleşmiş çok yüksek satışların beklenen değeri gereğinden fazla yükseltmesini engellemektir.

### 📈 Sapma Yüzdesinin Hesaplanması
Gerçek satış ile beklenen satış arasındaki fark yüzde olarak hesaplanır.

### 🚨 Anomali Durumları
Anomali gününün gerçek satış miktarı ile beklenen satış miktarı karşılaştırılır.

```text
Gerçek Satış > Beklenen Satış
              ↓
           SIÇRAMA
```

```text
Gerçek Satış < Beklenen Satış
              ↓
            DÜŞÜŞ
```

```text
Gerçek Satış = Beklenen Satış
              ↓
           ANOMALİ
```

Beklenen satış değeri `0` olduğunda ise satış gerçekleşmişse sistem bunu **SIÇRAMA** olarak değerlendirir.

### ⚠️ Anomali Şiddetinin Belirlenmesi
Anomalinin şiddeti gerçek satış ile beklenen satış arasındaki yüzde sapmaya göre belirlenir.

```text
Sapma ≥ %100
     ↓
  KRİTİK
```

```text
%50 ≤ Sapma < %100
     ↓
   YÜKSEK
```

```text
%25 ≤ Sapma < %50
     ↓
    ORTA
```

```text
Sapma < %25
     ↓
   DÜŞÜK
```

### 🧪 İstatistiksel Filtreleme

ML.NET tarafından tespit edilen her nokta doğrudan anomali olarak kabul edilmez.
Modelin ürettiği:

* `IsAnomaly`
* `Score`
* `PValue`

değerleri kontrol edilir.
Bir noktanın sonuçlara dahil edilmesi için:
```text
IsAnomaly = true
        +
PValue ≤ 0.05
        ↓
Geçerli Anomali
```

`PValue > 0.05` olan noktalar istatistiksel olarak yeterince anlamlı kabul edilmediğinden sonuç listesinden çıkarılır.

### 📊 Dashboard'da Gösterilen Bilgiler
Her tespit edilen anomali için aşağıdaki bilgiler oluşturulur:

* 📅 Satış tarihi
* 🌍 Ülke
* 🏙️ Şehir
* 📦 Ürün
* 🔢 Gerçek satış miktarı
* 🎯 Beklenen satış miktarı
* 📈 Satış değişimi
* 📊 Değişim yüzdesi
* 🧮 Anomali skoru
* 📉 P-Value
* 🚨 Anomali durumu
* ⚠️ Anomali şiddeti

Sonuçlar **en büyük yüzde sapmadan en küçüğe doğru** sıralanır. Böylece en önemli satış anomalileri dashboard üzerinde önce gösterilir.

<img width="1920" height="1590" alt="localhost_7189_Anomalies_Index" src="https://github.com/user-attachments/assets/ba65e03c-e31b-4b61-aca6-f8810429d788" />

## 6️⃣ Görev 6: Clustering — Şehirlerin Satış Davranışlarının Kümelenmesi

* **Algoritma:** `KMeans`
* **Amaç:** Benzer satış davranışına sahip şehirleri aynı kümelerde toplamak.
* **Küme Sayısı:** `2`
* **ML.NET:** `Microsoft.ML`
* **Tekrarlanabilirlik:** `seed = 42`

Şehirlerin satış kayıtları şehir bazında gruplanır ve her şehir için satış davranışını temsil eden özellikler hesaplanır.

### 📊 Kullanılan Şehir Özellikleri

KMeans modeline aşağıdaki 7 özellik verilir:

* 📊 Ortalama günlük satış miktarı
* 💰 Ortalama sipariş tutarı
* 🏷️ Kampanyalı satış oranı
* 📉 Ortalama indirim oranı
* 📦 Kategori sayısı
* 🥇 En çok satan kategorinin satış oranı
* 🗂️ Kategori çeşitliliği

### 📈 Ortalama Günlük Satışın Hesaplanması
Şehrin toplam satış miktarı, satış yapılan farklı gün sayısına bölünerek ortalama günlük satış miktarı hesaplanır.
Aktif gün sayısı aynı tarihteki kayıtlar tekrar sayılmayacak şekilde `Distinct()` kullanılarak hesaplanır.
Büyük satış değerlerinin model üzerindeki etkisini azaltmak için ortalama günlük satış miktarına logaritmik dönüşüm uygulanır.

### 💰 Ortalama Sipariş Tutarı

Şehirdeki satış kayıtlarının `TotalAmount` değerlerinin ortalaması alınır.
Büyük değerlerin KMeans üzerindeki etkisini azaltmak amacıyla logaritmik dönüşüm uygulanır.

### 🏷️ Kampanyalı Satış Oranı

Şehirdeki kampanyalı satışların toplam satış kayıtlarına oranı hesaplanır.
Bu özellik şehirlerin kampanyalara olan duyarlılığını temsil eder.

### 📉 Ortalama İndirim Oranı
Şehirdeki satış kayıtlarının `DiscountRate` değerlerinin ortalaması alınır.
Bu değer, şehirde uygulanan indirimlerin genel seviyesini temsil eder.

### 📦 Kategori Sayısı
Şehirde bulunan farklı kategoriler gruplanarak toplam kategori sayısı hesaplanır.


### 🥇 En Çok Satan Kategori Oranı
Kategoriler satış miktarına göre sıralanır ve en çok satan kategori belirlenir.
Bu kategorinin toplam satış içerisindeki oranı hesaplanır.
Bu değer yüksek olduğunda satışların belirli bir kategori üzerinde yoğunlaştığı anlaşılır.

### 🗂️ Kategori Çeşitliliği
Kategori çeşitliliğini ölçmek için **Entropy (Entropi)** kullanılır.
Her kategorinin toplam satış içerisindeki oranı hesaplanır.
Kategoriler arasındaki satış dağılımı daha dengeli oldukça kategori çeşitliliği değeri yükselir.

### 🔄 KMeans İçin Özelliklerin Hazırlanması
Hesaplanan 7 özellik `Features` isimli tek bir özellik vektöründe birleştirilir.
`NormalizeMinMax` kullanılarak özellikler 0–1 aralığına getirilir.
Bu sayede farklı ölçeklerde bulunan özelliklerin model üzerindeki etkisi dengelenir.

### 📊 Şehir Sonuçlarının Hesaplanması

Her şehir için aşağıdaki bilgiler oluşturulur:

* 🏙️ Şehir
* 🔢 Küme numarası
* 📦 Toplam satış miktarı
* 💵 Ortalama birim fiyat
* 💰 Toplam satış tutarı
* 🧾 Ortalama sipariş tutarı
* 💵 Toplam ciro
* 🏷️ Kampanyalı satış oranı
* 📦 Kategori sayısı
* 🥇 En çok satan kategori
* 📊 En çok satan kategorinin oranı
* 🗂️ Kategori çeşitliliği

Ayrıca her şehrin kategori bazındaki satış dağılımı da hesaplanır.

### 📊 Kategori Dağılımı

Şehirdeki kategoriler satış miktarına göre sıralanır.

Her kategori için:

* 📦 Satış miktarı
* 💰 Kategori cirosu
* 📊 Toplam satış içerisindeki yüzdesi
hesaplanır.

<img width="1920" height="2681" alt="localhost_7189_Clusters_Index" src="https://github.com/user-attachments/assets/adc99e75-98ee-4dc8-b52c-22b56387a687" />



## 💡 Performans ve Veri İşleme Yaklaşımı

Proje genelinde veri erişimi ve makine öğrenmesi süreçlerinde performans dikkate alınmıştır.

* `AsNoTracking()` kullanılarak yalnızca okunacak verilerde Entity Framework takip maliyeti azaltılır.
* `ToListAsync()` kullanılarak veritabanı işlemleri asenkron gerçekleştirilir.
* Boş şehir ve kategori bilgisine sahip kayıtlar filtrelenir.
* LINQ kullanılarak şehir ve kategori bazında veri gruplama işlemleri gerçekleştirilir.
* ML.NET'e aktarılmadan önce gerekli özellikler hazırlanır.
* Büyük sayısal değerlerin model üzerindeki etkisini azaltmak için logaritmik dönüşüm uygulanır.
* `NormalizeMinMax` ile model özellikleri normalize edilir.
* `seed: 42` kullanılarak KMeans sonuçlarının tekrarlanabilir olması sağlanır.
* Servis katmanı sayesinde veri işleme ve makine öğrenmesi işlemleri Controller'dan ayrılır.
* Dependency Injection kullanılarak `AppDbContext` servis içerisinde yönetilir.

## 📂 Proje Katmanları ve Mimari Yapı

```text
ECommerceSalesIntelligence/
│
├── Context/
│   └── AppDbContext.cs
│
├── Controllers/
│   ├── DashboardController.cs
│   ├── ForecastingController.cs
│   ├── BinaryClassificationController.cs
│   ├── MulticlassClassificationController.cs
│   ├── AnomaliesController.cs
│   └── ClustersController.cs
│   └── SalesIntelligenceController.cs
│
├── Entities/
│   └── SalesRecord.cs
│
├── Migrations/
|
├── Models/
│   └── Anomaly/
│       ├── SalesAnomalyInput.cs
│       ├── SalesAnomalyPrediction.cs
│       └── SalesAnomalyResultViewModel.cs
│   └── Classification/
│       ├── ClassificationDashboardViewModel.cs
│       ├── MulticlassClassificationViewModel.cs
│       └── SalesClassificationInput.cs
│       └── SalesClassificationPrediction.cs
│       └── SalesMulticlassInput.cs
│       └── SalesMulticlassPrediction.cs
│   └── Cluster/
│       ├── SalesClusterInput.cs
│       ├── SalesClusterPrediction.cs
│       └── ClusterResultViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── SalesData.cs
│   ├── SalesPrediction.cs
│
├── Services/
│   ├── AnomalyDetectionService.cs
│   ├── BinaryClassificationService.cs
│   └── ClusteringService.cs
│   ├── DashboardService.cs
│   ├── ForecastingService.cs
│   ├── MulticlassClassificationService.cs
│
├── Views/
│   ├── Anomalies/
│   │   └── Index.cshtml
│   ├── BinaryClassification/
│   │   └── Index.cshtml
│   └── Clusters/
│       └── Index.cshtml
│   ├── Dashboard/
│   │   └── Index.cshtml
│   ├── Forecasting/
│   │   └── Index.cshtml
│   ├── MulticlassClassification/
│   │   └── Index.cshtml
│   ├── Shared
│
├── appsettings.json
│
└── Program.cs
