using AutoMapper;
using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    public class ClassificationService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MLContext _mlContext;

        public ClassificationService(AppDbContext context, IMapper mapper, MLContext mlContext)
        {
            _context = context;
            _mapper = mapper;
            _mlContext = mlContext;
        }

        public SalesClassificationPrediction TrainAndEvaluateAsync()
        {
            IEnumerable<SalesClassificationInput> GetStreamingData() //bu metotla veritabanından verileri parça parça çekiyoruz ve RAMi yormuyoruz
            {
                foreach (var record in _context.SalesRecords.AsNoTracking().AsEnumerable()) //AsNoTracking, veritabanından çekilen verilerin değiştirilemeyeceğini belirtiyor performans artışı sağlar
                                                                                            //AsEnumarable: veritabanından çekilen verileri IEnumerable tipine dönüştürür
                                                                                            //Ienumarable: veritabanından çekilen verileri parça parça döndürür RAMi şişirmez 
                {
                    yield return _mapper.Map<SalesClassificationInput>(record); //yield: veriyi parça parça döndürür
                }
            }

            //IDataViewa standart IEnumerable akışıyla yükleme yapıyoruz
            IDataView dataView = _mlContext.Data.LoadFromEnumerable<SalesClassificationInput>(GetStreamingData());

            // TRAIN / TEST AYRIMI (%80 Eğitim, %20 Test)
            var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            //Pipeline ve Model Eğitimi
            var pipeline = _mlContext.Transforms.Concatenate("Features", //Features adında bir feature vektörü oluşturuyoruz
                                                       nameof(SalesClassificationInput.UnitPrice), //Feature vektörüne ekleyeceğimiz özellikler ücret, miktar ve indirim oranı
                                                       nameof(SalesClassificationInput.Quantity),
                                                       nameof(SalesClassificationInput.DiscountRate))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));  //BinaryClassification: ikili sınıflandırma yapıyoruz yani satışın başarılı olup olmadığını tahmin ediyoruz
                                                                                                                                                    //trainers : modelin eğitileceği algoritma
                                                                                                                                                    //SdcaLogisticRegression: Stochastic Dual Coordinate Ascent algoritmasını kullanarak lojistik regresyon modeli eğitiyoruz
                                                                                                                                                    //Lojistik Regresyon: bağımlı değişkenin kategorik olduğu durumlarda kullanılan bir regresyon modeli
                                                                                                                                                    //Bağımlı değişkenin 0 veya 1 olduğu durumlarda kullanılır. Örneğin, satışın başarılı olup olmadığını tahmin etmek için kullanılır

            // Modeli sadece eğitim verisiyle (%80) eğitiyoruz
            var model = pipeline.Fit(splitData.TrainSet); //modeli eğitiyoruz

            //MODEL DEĞERLENDİRME METRİKLERİ 
            var transformedTestSet = model.Transform(splitData.TestSet);
            var metrics = _mlContext.BinaryClassification.Evaluate(transformedTestSet, labelColumnName: "Label");


            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesClassificationInput, SalesClassificationPrediction>(model); // PredictionEngine: modelin tahmin yapabilmesi için kullanılan bir sınıf
            //CreatePredictionEngine: modelin tahmin yapabilmesi için bir PredictionEngine oluşturuyoruz
            //SalesClassificationInput: modelin tahmin yapacağı veri tipi
            //SalesClassificationPrediction: modelin tahmin edeceği sonuç tipi


            //tahmin yapılacak veri tipinin özelliklerini belirliyoruz
            //sample nesnesi sadece modelin doğru çalışıp çalışmadığını yerinde test edilmesini sağlar
            var sample = new SalesClassificationInput
            {
                UnitPrice = 1500f,
                Quantity = 10f,
                DiscountRate = 0.20f,
                IsCampaign = true
            };

            return predictionEngine.Predict(sample); //modelin tahmin yapmasını sağlıyoruz ve tahmin sonucunu döndürüyoruz
        }
    }
}