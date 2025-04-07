namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class DashboardViewModel
    {
        public int TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFats { get; set; }
        public List<FoodLog> FoodLogs { get; set; }
        public List<ChartDataItem> ChartData { get; set; }  // Changed to List<ChartDataItem>

        // Add filters to the model to pass selected filters to the view
        public string MealTypeFilter { get; set; }
        public DateTime? StartDateFilter { get; set; }
        public DateTime? EndDateFilter { get; set; }
        public decimal[] PieChartData { get; set; }
        public DashboardViewModel()
        {
            FoodLogs = new List<FoodLog>();
            ChartData = new List<ChartDataItem>();
        }
    }

    public class ChartDataItem
    {
        public string Date { get; set; }
        public int Calories { get; set; }
    }
}
