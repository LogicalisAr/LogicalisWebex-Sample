using Microsoft.Extensions.Logging;
using Microsoft.SallyBot.Dialogs;
using Newtonsoft.Json;
using SallyBot.DTOs;
using SallyBot.Repository.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SallyBot.Services
{
    public class UserServices
    {
        private readonly ApplicationContext _context;
        private readonly UserRepository _userRepository;
        private readonly WebexMeetingService _webexMeetingService;
        private readonly ProactiveMessageService _proactiveMessageService;
        //private readonly ILogger _logger;
        public UserServices(ApplicationContext context,
                                ProactiveMessageService proactiveMessageService,
                                WebexMeetingService webexMeetingService//,
                                /*ILogger logger*/)
        {
            _context = context;
            //_logger = logger;
            _webexMeetingService = webexMeetingService;
            _proactiveMessageService = proactiveMessageService;
            _userRepository = new UserRepository(_context);
        }

        public Task saveConversationId(string userMail, string conversationId)
        {
            User user = this.getUserByEmail(userMail);

            if (user == null)
            {
                user = new User();
                user.Name = userMail.Substring(0, userMail.LastIndexOf("@"));
                user.Email = userMail;
                this.saveUser(user);
            }

            user.ConversationID = conversationId;
            _userRepository.Update(user);

            return Task.FromResult<User>(user);
        }

        public void saveUser(User user)
        {
            _userRepository.Add(user);
        }

        public async Task<bool> generateWebexToken(string code)
        {
            WebexTokenDTO webexTokenDTO = await this.getAccessToken(code);
            return await updateTokenInfoByAccessToken(webexTokenDTO, true);
        }

        public async Task<bool> refreshWebexToken()
        {
            DateTime today = DateTime.Now;
            WebexTokenDTO webexTokenDTO = null;

            List<User> users = this.getUsersExpiredTokenByDate(today);

            List<bool> pendingTasks = new List<bool>();

            foreach (User user in users)
            {
                //_logger.LogInformation("User: " + user.Email);
                webexTokenDTO = await this.refreshAccessToken(user.TokenRefreshWebex);
                pendingTasks.Add(await updateTokenInfoByAccessToken(webexTokenDTO, false));
            }

            bool allOk = !pendingTasks.Where(x => !x).Any();

            return await Task.FromResult<bool>(allOk);
        }

        public async Task<bool> cleanRefreshWebexTokensExpired()
        {
            DateTime today = DateTime.Now;
            WebexTokenDTO webexTokenDTO = new WebexTokenDTO();

            List<User> users = this.getUsersExpiredRefreshTokenByDate(today);

            List<bool> pendingTasks = new List<bool>();

            foreach (User user in users)
                pendingTasks.Add(updateTokenInfoByUser(webexTokenDTO, false, user));

            bool allOk = !pendingTasks.Where(x => !x).Any();

            return await Task.FromResult<bool>(allOk);
        }

        private async Task<bool> updateTokenInfoByAccessToken(WebexTokenDTO webexTokenDTO, bool sendNotification)
        {
            bool success = false;
            if (webexTokenDTO != null)
            {
                WebexPeopleMeDTO webexPeopleMeDTO = await this.getPeopleByAccessToken(webexTokenDTO.access_token);
                if (webexPeopleMeDTO != null)
                {
                    User user = this.getUserByEmail(webexPeopleMeDTO.emails.FirstOrDefault());
                    if (user != null)
                    {
                        success = updateTokenInfoByUser(webexTokenDTO, sendNotification, user);
                    }
                }
            }

            return success;
        }

        private bool updateTokenInfoByUser(WebexTokenDTO webexTokenDTO, bool sendNotification, User user)
        {
            bool success;
            DateTime date = DateTime.Now;
            user.TokenWebex = webexTokenDTO.access_token;
            if (webexTokenDTO.expires_in.HasValue)
                user.TokenWebexExpires = date.AddSeconds(webexTokenDTO.expires_in.Value);
            else
                user.TokenWebexExpires = null;

            user.TokenRefreshWebex = webexTokenDTO.refresh_token;

            if (webexTokenDTO.expires_in.HasValue)
                user.TokenRefreshWebexExpires = date.AddSeconds(webexTokenDTO.refresh_token_expires_in.Value);
            else
                user.TokenRefreshWebexExpires = null;

            _userRepository.Update(user);

            success = true;
            if (sendNotification)
                sendNotificationWebexTokenUpdated(user);
            return success;
        }

        private void sendNotificationWebexTokenUpdated(User user)
        {
            if (user.ConversationID != null)
            {
                NotificationDTO notificationDTO = new NotificationDTO();
                notificationDTO.conversationId = user.ConversationID;
                notificationDTO.message = "";
                notificationDTO.activityCode = "webex-token-success";
                _proactiveMessageService.sendMessage(notificationDTO);
            }
        }

        public async Task<WebexTokenDTO> getAccessToken(string code)
        {
            string contents = await _webexMeetingService.getOauthTOken(code);
            //_logger.LogDebug("getAccessToken: contents" + contents?.ToString());

            if (contents != null)
            {
                WebexTokenDTO webexTokenDTO = JsonConvert.DeserializeObject<WebexTokenDTO>(contents);
                //_logger.LogDebug("webexTokenDTO: " + webexTokenDTO?.access_token);

                if (webexTokenDTO != null && webexTokenDTO.access_token != null)
                    return webexTokenDTO;

                //_logger.LogError("webexTokenDTO.message:" + webexTokenDTO?.message);
            }

            return null;
        }

        public async Task<WebexTokenDTO> refreshAccessToken(string refresh_token)
        {
            string contents = await _webexMeetingService.refreshOauthTOken(refresh_token);
            //_logger.LogDebug("refreshAccessToken: contents" + contents?.ToString());

            if (contents != null)
            {
                WebexTokenDTO webexTokenDTO = JsonConvert.DeserializeObject<WebexTokenDTO>(contents);
                //_logger.LogDebug("webexTokenDTO: " + webexTokenDTO?.access_token);

                if (webexTokenDTO != null && webexTokenDTO.access_token != null)
                    return webexTokenDTO;

                //_logger.LogError("webexTokenDTO.message:" + webexTokenDTO?.message);
            }

            return null;
        }

        public async Task<WebexPeopleMeDTO> getPeopleByAccessToken(string accessToken)
        {
            var contents = await _webexMeetingService.getPeopleMe(accessToken);

            if (contents != null)
            {
                WebexPeopleMeDTO webexPeopleMeDTO = JsonConvert.DeserializeObject<WebexPeopleMeDTO>(contents);
                //_logger.LogDebug("webexPeopleMeDTO.emails.Count: " + webexPeopleMeDTO?.emails?.Count);
                //_logger.LogDebug("webexPeopleMeDTO.message: " + webexPeopleMeDTO?.message);

                if (webexPeopleMeDTO?.emails?.Count > 0)
                    return webexPeopleMeDTO;

                //_logger.LogError(webexPeopleMeDTO?.message);
            }

            return null;
        }

        public string getAuthorizeURL()
        {
            return _webexMeetingService.getAuthorizeURL();
        }

        public User getUserByEmail(string userMail)
        {
            if (userMail != null)
                return _userRepository.Find(x => String.Equals(x.Email.ToLower(), userMail.ToLower())).Cast<User>().FirstOrDefault();
            return null;
        }

        public List<User> getUsersExpiredTokenByDate(DateTime date)
        {
            return _userRepository
                    .Find(t => t.TokenWebexExpires.Value.Date <= date.Date)
                    .ToList();
        }

        public List<User> getUsersExpiredRefreshTokenByDate(DateTime date)
        {
            return _userRepository
                    .Find(t => t.TokenRefreshWebexExpires.Value.Date <= date.Date)
                    .ToList();
        }
    }
}
