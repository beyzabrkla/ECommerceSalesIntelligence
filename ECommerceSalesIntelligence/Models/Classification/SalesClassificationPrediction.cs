using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesClassificationPrediction
    {
        // ============================================================
        // ŞEHİR
        // ============================================================

        public string City { get; set; } = "";

        // ============================================================
        // ÜRÜN
        // ============================================================

        public string ProductName { get; set; } = "";

        // ============================================================
        // GEÇMİŞ SATIŞLAR
        // ============================================================

        public float LastThreeMonthsSales { get; set; }

        public float LastMonthSales { get; set; }

        public float ThreeMonthAverage { get; set; }

        // ============================================================
        // TAHMİN
        // ============================================================

        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        // ============================================================
        // PROBABILITY
        // ============================================================

        public float Probability { get; set; }

        // ============================================================
        // SCORE
        // ============================================================

        public float Score { get; set; }

        // ============================================================
        // HEDEF AY
        // ============================================================

        public string TargetMonth { get; set; } = "";
    }
}