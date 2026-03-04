namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class NutritionGoalService
    {
        public int CalculateBMR(double weightKg, double heightCm, int age, string gender)
        {
            if (gender.ToLower() == "male")
                return (int)(10 * weightKg + 6.25 * heightCm - 5 * age + 5);
            else
                return (int)(10 * weightKg + 6.25 * heightCm - 5 * age - 161);
        }

        public int CalculateTDEE(int bmr, double activityMultiplier)
        {
            return (int)(bmr * activityMultiplier);
        }

        public int AdjustForGoal(int tdee, string goal)
        {
            return goal.ToLower() switch
            {
                "cut" => tdee - 500,
                "bulk" => tdee + 500,
                _ => tdee
            };
        }

        public (int protein, int carbs, int fats) CalculateMacros(int calories, string goal)
        {
            double proteinRatio = goal == "bulk" ? 0.30 : 0.35;
            double fatsRatio = 0.25;
            double carbsRatio = 1 - proteinRatio - fatsRatio;

            int proteinCalories = (int)(calories * proteinRatio);
            int fatCalories = (int)(calories * fatsRatio);
            int carbCalories = (int)(calories * carbsRatio);

            return (
                protein: proteinCalories / 4,
                carbs: carbCalories / 4,
                fats: fatCalories / 9
            );
        }
    }
}