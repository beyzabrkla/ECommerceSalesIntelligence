using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesClassificationInput
    {
        // ============================================================
        // CASE: BELİRLENEN ŞEHİR
        // ============================================================

        [LoadColumn(0)]
        public string City { get; set; } = "";

        // ============================================================
        // CASE: ÜRÜN
        // ============================================================

        [LoadColumn(1)]
        public string ProductName { get; set; } = "";

        // ============================================================
        // CASE: SON 3 AYLIK SATIŞ
        // ============================================================

        [LoadColumn(2)]
        public float LastThreeMonthsSales { get; set; }

        // ============================================================
        // CASE: SON AY SATIŞ
        // ============================================================

        [LoadColumn(3)]
        public float LastMonthSales { get; set; }

        // ============================================================
        // CASE: 3 AYLIK ORTALAMA
        // ============================================================

        [LoadColumn(4)]
        public float ThreeMonthAverage { get; set; }

        // ============================================================
        // CASE: HEDEF AY
        // ============================================================

        [LoadColumn(5)]
        public string TargetMonth { get; set; } = "";

        // ============================================================
        // GERÇEK SATIŞ
        // Sadece eğitim sırasında kullanılır.
        // ============================================================

        [LoadColumn(6)]
        public float TargetQuantity { get; set; }

        // ============================================================
        // LABEL
        //
        // TargetQuantity >= 7000 => EVET
        // TargetQuantity < 7000  => HAYIR
        // ============================================================

        [ColumnName("Label")]
        [LoadColumn(7)]
        public bool Label { get; set; }
    }
}