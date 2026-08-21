using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesClassificationInput
    {
        [LoadColumn(0)]
        public string City { get; set; } = "";

        [LoadColumn(1)]
        public string ProductName { get; set; } = "";

        [LoadColumn(2)]
        public float LastThreeMonthsSales { get; set; }

        [LoadColumn(3)]
        public float LastMonthSales { get; set; }

        [LoadColumn(4)]
        public float ThreeMonthAverage { get; set; }

        [LoadColumn(5)]
        public string TargetMonth { get; set; } = "";

        [LoadColumn(6)]
        public float TargetQuantity { get; set; }

        [ColumnName("Label")]
        [LoadColumn(7)]
        public bool Label { get; set; }
    }
}