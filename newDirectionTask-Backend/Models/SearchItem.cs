using System;

namespace newDirectionTask_Backend.Models
{
    public class SearchItem
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Url { get; set; }

        public string Keyword { get; set; }

        public int Count { get; set; }
   
    }
}