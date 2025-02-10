using FlomtManager.Core.Entities;

namespace FlomtManager.Core.Models
{
    public class DataGroupValues
    {
        public required DateTime DateTime { get; set; }
        public required Parameter[] Parameters { get; set; }
        public required object[] Values { get; set; }
    }
}
