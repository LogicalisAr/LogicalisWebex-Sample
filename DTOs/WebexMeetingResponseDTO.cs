using System;

namespace SallyBot.DTOs
{
    public class WebexMeetingResponseDTO
    {
        public string id { get; set; }
        public string meetingNumber { get; set; }
        public string title { get; set; }
        public string password { get; set; }
        public string meetingType { get; set; }
        public string state { get; set; }
        public string timezone { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string hostUserId { get; set; }
        public string hostDisplayName { get; set; }
        public string hostEmail { get; set; }
        public string hostKey { get; set; }
        public string webLink { get; set; }
        public string sipAddress { get; set; }
        public string dialInIpAddress { get; set; }
        public string message { get; set; }
    }
}
