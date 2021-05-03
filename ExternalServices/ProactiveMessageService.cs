using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SallyBot.DTOs;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SallyBot.Services
{
    public class ProactiveMessageService
    {
        private readonly string _webHookBotProactiveMessages;
        public ProactiveMessageService(IConfiguration configuration)
        {
            _webHookBotProactiveMessages = configuration["WebHookBotProactiveMessages"];
        }

        public async Task<bool> sendMessage(NotificationDTO notificationDTO)
        {
            bool status = false;

            HttpClient httpClient = new HttpClient();

            var response = await httpClient.PostAsync(_webHookBotProactiveMessages, new StringContent(JsonConvert.SerializeObject(notificationDTO), System.Text.Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contents = await response.Content.ReadAsStringAsync();
                status = JsonConvert.DeserializeObject<bool>(contents);
            }

            return status;
        }

    }
}
