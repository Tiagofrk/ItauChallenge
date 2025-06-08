using System;

namespace ItauChallenge.QuotesConsumer
{
    public class KafkaMessageDto
    {
        public string MessageId { get; set; }
        public string AssetTicker { get; set; } // Or AssetId if the message provides the numeric ID
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; } // Quote timestamp
    }
}
