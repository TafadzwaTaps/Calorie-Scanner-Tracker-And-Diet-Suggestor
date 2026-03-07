namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.DTOs
{
    public class MealDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string MealType { get; set; } = "Lunch";
        public string? ImageUrl { get; set; }
    }

    public class FoodAnalysisResult
    {
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string? WarningMessage { get; set; }
        public string? Error { get; set; }
        public string? FoodName { get; set; }
    }
}