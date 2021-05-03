using System;
using System.Collections.Generic;

namespace SallyBot.DTOs
{
    public class WebexMeDTO
    {
        public string id { get; set; }
        public List<string> emails { get; set; }
        public List<string> phoneNumbers { get; set; }
        public string displayName { get; set; }
        public string nickName { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string orgId { get; set; }
        public DateTime created { get; set; }
        public DateTime lastActivity { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public string message { get; set; }
    }
}
