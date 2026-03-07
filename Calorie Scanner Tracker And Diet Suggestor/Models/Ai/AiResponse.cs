namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models.AI
{
    public class AiResponse
    {
        public List<DetectionResult> Detections { get; set; } = new();

        public string? WarningMessage { get; set; }

        public string? Error { get; set; }
    }

    public class DetectionResult
    {
        public string Food { get; set; }

        public double Confidence { get; set; }

        public BoundingBox Box { get; set; }

        public NutritionValues Nutrition { get; set; }
    }

    public class BoundingBox
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }

    public class NutritionValues
    {
        public double Calories { get; set; }

        public double Protein { get; set; }

        public double Carbs { get; set; }

        public double Fats { get; set; }
    }
}