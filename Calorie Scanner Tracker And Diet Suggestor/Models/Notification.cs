namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }

        public Notification()
        {
            
        }
    }
}
