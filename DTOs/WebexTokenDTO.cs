using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SallyBot.DTOs
{
    public class WebexTokenDTO
    {
        public string access_token { get; set; }
        public long? expires_in { get; set; }
        public string refresh_token { get; set; }
        public long? refresh_token_expires_in { get; set; }
        public string message { get; set; }
        //public List<JObject> errors { get; set; }
        public string trackingId { get; set; }
    }
}
