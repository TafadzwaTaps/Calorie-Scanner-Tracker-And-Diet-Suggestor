using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models.AI;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class NutritionProcessor
    {
        public AiResponse Clean(AiResponse result)
        {
            foreach (var item in result.Detections)
            {
                if (item.Nutrition.Calories < 0)
                    item.Nutrition.Calories = 0;

                if (item.Nutrition.Protein < 0)
                    item.Nutrition.Protein = 0;

                if (item.Nutrition.Carbs < 0)
                    item.Nutrition.Carbs = 0;

                if (item.Nutrition.Fats < 0)
                    item.Nutrition.Fats = 0;

                // round values
                item.Nutrition.Calories = Math.Round(item.Nutrition.Calories, 1);
                item.Nutrition.Protein = Math.Round(item.Nutrition.Protein, 1);
                item.Nutrition.Carbs = Math.Round(item.Nutrition.Carbs, 1);
                item.Nutrition.Fats = Math.Round(item.Nutrition.Fats, 1);
            }

            return result;
        }
    }
}
