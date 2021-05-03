// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters.Webex;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SallyBot.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SallyBot.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly WebexAdapter _webexAdapter;
        private readonly string _appId;
        private string _message = null;
        private string _activityCode = null;
        private string _channelId = null;

        private readonly ILogger<UserController> _logger;

        public NotifyController(IBotFrameworkHttpAdapter adapter,
                                WebexAdapter webexAdapter,
                                IConfiguration configuration,
                                ILogger<UserController> logger)
        {
            _logger = logger;
            _adapter = adapter;
            _webexAdapter = webexAdapter;
            _appId = configuration["MicrosoftAppId"];

            // If the channel is the Emulator, and authentication is not in use,
            // the AppId will be null.  We generate a random AppId for this case only.
            // This is not required for production, since the AppId will have a value.
            if (string.IsNullOrEmpty(_appId))
            {
                _appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
            }
        }

        [HttpPost]
        public async Task<IActionResult> sendNotification([FromBody] NotificationDTO notificationDTO)
        {
            if (notificationDTO.conversationId != null && notificationDTO.message != null)
            {
                ConversationAccount conversation = new ConversationAccount(null, null, notificationDTO.conversationId);

                ConversationReference conversationReference = new ConversationReference(null, null, null, conversation);

                _message = notificationDTO.message;
                _activityCode = notificationDTO.activityCode;
                _channelId = conversationReference.ChannelId;
                if (conversationReference.ChannelId == "webex" || conversationReference.ChannelId == null)
                    await ((WebexAdapter)_webexAdapter).ContinueConversationAsync(_appId, conversationReference, sendMessageCallback, default(CancellationToken));
                else
                    return Problem("This functionality is only available in Webex");

                return Ok(true);
            }
            return Problem("It's needed send \"conversationId\" and \"message\"");
        }

        private async Task sendMessageCallback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Tu codigo de acceso de webex fue generado con exito. Para probar escribir \"Crear reservacion\".");
        }
    }
}