using System;

namespace ItauChallenge.Domain
{
    public class Asset
    {
        public int Id { get; set; }
        public string Ticker { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedDth { get; set; }
    }
}
