namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }

        public Message()
        {

        }
    }
}
