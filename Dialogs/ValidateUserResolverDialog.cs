// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using SallyBot;
using SallyBot.DTOs;
using SallyBot.Services;
using SallyBot.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SallyBot.Dialogs
{
    public class ValidateUserResolverDialog : CancelAndHelpDialog
    {

        private static String debugName = "Lucas Rodriguez";
        private static String debugMail = "lucas.rodriguez@LA.LOGICALIS.COM";

        private readonly WebexMeetingService _webexMeetingService;
        private readonly UserServices _userServices;
        private readonly WebexService _webexService;
        private readonly CardBuilder _cardBuilder;

        public ValidateUserResolverDialog(WebexMeetingService webexMeetingService,
                                            WebexService webexService,
                                            UserServices userServices)
            : base(nameof(ValidateUserResolverDialog))
        {
            _webexMeetingService = webexMeetingService;
            _webexService = webexService;
            _userServices = userServices;
            _cardBuilder = new CardBuilder();

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                LoadUserStepAsync,
                //ValidateAccessStepAsync,
                CheckCredentialsStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> LoadUserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userDetails = (UserDetails)stepContext.Options;
            switch (stepContext.Context.Activity.ChannelId)
            {
                case "webex":
                    WebexMeDTO webexMeDTO = await _webexService.getUserDataAsync(stepContext.Context.Activity.From.Id);
                    userDetails.UserName = webexMeDTO.displayName;
                    userDetails.UserEmail = webexMeDTO.emails[0];
                    break;
                case "emulator":
                    userDetails.UserName = debugName;
                    userDetails.UserEmail = debugMail;
                    break;
                default:  //MS-TEAMS
                    var member = await TeamsInfo.GetMemberAsync(stepContext.Context, stepContext.Context.Activity.From.Id, cancellationToken);
                    userDetails.UserName = member.Name;
                    userDetails.UserEmail = member.Email;
                    break;
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> CheckCredentialsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userDetails = (UserDetails)stepContext.Options;

            UserValidTokenDTO userValidTokenDTO = this.checkValidToken(userDetails.UserEmail);

            if (userValidTokenDTO != null && !userValidTokenDTO.valid_token)
            {
                Dictionary<String, String> values = new Dictionary<String, String>();
                values.Add("message", "Para continuar, precisamos que nos des algunos permisos haciendo click aca:");
                values.Add("link", userValidTokenDTO.url_get_token);
                var bookApprovedCard = _cardBuilder.getAttachment("requestPermissionCard", stepContext.Context.Activity.ChannelId, values);

                await stepContext.Context.SendActivityAsync((Bot.Schema.Activity)MessageFactory.Attachment(bookApprovedCard));
                return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "", cancellationToken);
            }
            else if (userValidTokenDTO != null && userValidTokenDTO.valid_token)
            {
                User user = _userServices.getUserByEmail(userDetails.UserEmail);
                userDetails.TokenWebex = user.TokenWebex;
            }
            //}
            return await stepContext.NextAsync(stepContext.Options, cancellationToken);
        }

        private UserValidTokenDTO checkValidToken(string userEmail)
        {
            bool isValidToken = false;
            string urlGetToken = null;
            //Retrieves oauth code to generate tokens for users
            User user = _userServices.getUserByEmail(userEmail);

            if (user != null)
            {
                if (user.TokenWebex == null)
                    urlGetToken = _userServices.getAuthorizeURL();
                else
                    isValidToken = true;

                UserValidTokenDTO userValidTokenDTO = new UserValidTokenDTO
                {
                    valid_token = isValidToken,
                    url_get_token = urlGetToken
                };

                return userValidTokenDTO;
            }
            return null;
        }
    }
}
