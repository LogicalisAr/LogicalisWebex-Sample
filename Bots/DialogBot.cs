// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SallyBot;
using SallyBot.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SallyBot.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly UserServices UserServices;
        protected readonly ILogger Logger;

        public DialogBot(ConversationState conversationState, UserState userState, UserServices userServices, T dialog, ILogger<DialogBot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            UserServices = userServices;
            Dialog = dialog;
            Logger = logger;
        }

        private void AddConversationReference(Activity activity)
        {
            ConversationReference conversationReference = activity.GetConversationReference();
            UserServices.saveConversationId(conversationReference.User.Name, conversationReference.Conversation.Id);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = turnContext.Activity;
            if (activity.Type == ActivityTypes.Event && turnContext.Activity.ChannelId == "webex")
            {
                if (activity.Text == "" && activity.Value != null)
                    HandleSubmitAction(turnContext, cancellationToken);
            }

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private void HandleSubmitAction(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Value != null)
            {
                var ans = (IDictionary<string, string>)turnContext.Activity.Value;
                var jsonSt = JsonConvert.SerializeObject(ans, Formatting.Indented);
                JObject json = JObject.Parse(jsonSt);
                turnContext.Activity.Value = json;
                turnContext.Activity.Type = "message";
                if (((dynamic)json).welcome_choice is JValue question)
                {
                    turnContext.Activity.Value = null;
                    turnContext.Activity.Text = (string)question.Value;
                }
            }
        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");
            turnContext.Activity.Locale = "es-ES";

            AddConversationReference(turnContext.Activity as Activity);

            // Get the state properties from the turn context.
            var conversationStateAccessors = ConversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }
    }
}
