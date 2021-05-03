// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using SallyBot.DTOs;
using SallyBot.Services;
using SallyBot.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SallyBot.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T>
        where T : Dialog
    {

        private static String debugName = "Lucas Rodriguez";
        private static String debugMail = "lucas.rodriguez@LA.LOGICALIS.COM";

        private readonly WebexService _webexService;

        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, UserServices userServices, T dialog, WebexService webexService, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, userServices, dialog, logger)
        {
            _webexService = webexService;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    UserDetails userDetails = await LoadUserStepAsync(turnContext, cancellationToken);
                    CardBuilder _cardBuilder = new CardBuilder();
                    Dictionary<String, String> values = new Dictionary<String, String>();
                    values.Add("welcomeUser", userDetails.UserName);
                    var welcomeCard = _cardBuilder.getAttachment("welcomeCard", turnContext.Activity.ChannelId, values);

                    await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(welcomeCard), cancellationToken);
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }

        private async Task<UserDetails> LoadUserStepAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            UserDetails userDetails = new UserDetails();
            switch (turnContext.Activity.ChannelId)
            {
                case "webex":
                    WebexMeDTO webexMeDTO = await _webexService.getUserDataAsync(turnContext.Activity.ChannelId);
                    userDetails.UserName = webexMeDTO.displayName;
                    userDetails.UserEmail = webexMeDTO.emails[0];
                    break;
                case "emulator":
                    userDetails.UserName = debugName;
                    userDetails.UserEmail = debugMail;
                    break;
                default:  //MS-TEAMS
                    var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.ChannelId, cancellationToken);
                    userDetails.UserName = member.Name;
                    userDetails.UserEmail = member.Email;
                    break;
            }
            return userDetails;
        }

    }
}
