// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using SallyBot.Dialogs;
using SallyBot.Services;
using SallyBot.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SallyBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BookingRecognizer _luisRecognizer;
        private readonly WebexService _webexService;
        private readonly WebexMeetingService _webexMeetingService;
        private readonly UserServices _userServices;
        private readonly CardBuilder _cardBuilder;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(BookingRecognizer luisRecognizer,
                            BookingAssetDialog bookingAssetDialog,
                            WebexService webexService,
                            WebexMeetingService webexMeetingService,
                            UserServices userServices,
                            ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _userServices = userServices;
            _luisRecognizer = luisRecognizer;
            _webexService = webexService;
            _webexMeetingService = webexMeetingService;
            _cardBuilder = new CardBuilder();
            Logger = logger;

            //AddDialog(new ValidateUserResolverDialog(_webexMeetingService, _webexService, new UserServices(/*context, */proactiveMessageService, webexMeetingService/*, logger*/), logger));
            AddDialog(new ValidateUserResolverDialog(_webexMeetingService, _webexService, _userServices/*, logger*/));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingAssetDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ValidateAccessStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.

            //return await stepContext.NextAsync(null, cancellationToken);
            if (stepContext.Options != null)
            {
                var messageText = stepContext.Options?.ToString() ?? "Bienvenido";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> ValidateAccessStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                var promptMessage = $"Lo siento, el servicio de interpretacion no esta configurado, contactese con el administrador. Gracias";
                return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }

            if (stepContext.Context.Activity.Text != null)
            {
                if (stepContext.Context.Activity.Text.ToLower().Contains("una sala"))
                    stepContext.Context.Activity.Text = stepContext.Context.Activity.Text.ToLower().Replace("una sala", "sala");

                if (stepContext.Context.Activity.Text.ToLower().Contains("proximo") || stepContext.Context.Activity.Text.ToLower().Contains("próximo"))
                {
                    stepContext.Context.Activity.Text = stepContext.Context.Activity.Text.ToLower().Replace("proximo", "");
                    stepContext.Context.Activity.Text = stepContext.Context.Activity.Text.ToLower().Replace("próximo", "");
                }

                // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
                var luisResult = await _luisRecognizer.RecognizeAsync<AssetsBooking>(stepContext.Context, cancellationToken);

                var userDetails = new UserDetails();
                userDetails.Activity = luisResult.TopIntent().intent;
                userDetails.LuisResult = luisResult;
                return await stepContext.BeginDialogAsync(nameof(ValidateUserResolverDialog), userDetails, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext == null || stepContext.Result == null)
                return await stepContext.NextAsync(false, cancellationToken);

            var userDetails = (UserDetails)stepContext.Result;
            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)

            //var luisResult = await _luisRecognizer.RecognizeAsync<AssetsBooking>(stepContext.Context, cancellationToken);
            var luisResult = userDetails.LuisResult;

            DateTime? luisDate = null;
            DateTime? luisStart = null;
            String luisDuration = null;
            if (luisResult.Date != null)
            {
                if (luisResult.Date == "PRESENT_REF")
                {
                    luisDate = DateTime.Now;
                }
                else
                {
                    Resolution resolution = TimexResolver.Resolve(new[] { luisResult.Date }, DateTime.Now);
                    Resolution.Entry lastDateDetected = resolution.Values.Last();
                    if (lastDateDetected.Type == "date")
                        luisDate = DateTime.ParseExact(lastDateDetected.Value, "yyyy-MM-dd", new CultureInfo("es-ES", true));
                    else if (lastDateDetected.Type == "datetime")
                        luisDate = DateTime.ParseExact(lastDateDetected.Value, "yyyy-MM-dd HH:mm:ss", new CultureInfo("es-ES", true));
                }
            }//TODO: When it enter in this "else if"?
            else if (luisResult.Start != null)
            {
                if (luisResult.Start == "PRESENT_REF")
                {
                    luisStart = DateTime.Now;
                }
                else
                {
                    Resolution resolution = TimexResolver.Resolve(new[] { luisResult.Start }, DateTime.Now);
                    Resolution.Entry lastDateDetected = resolution.Values.Last();
                    if (lastDateDetected.Type == "date")
                        luisStart = DateTime.ParseExact(lastDateDetected.Value, "yyyy-MM-dd", new CultureInfo("es-ES", true));
                    else if (lastDateDetected.Type == "datetime")
                        luisStart = DateTime.ParseExact(lastDateDetected.Value, "yyyy-MM-dd HH:mm:ss", new CultureInfo("es-ES", true));
                    else if (lastDateDetected.Type == "time")
                        if (luisResult.Duration == null && luisResult.Text.Contains("por"))
                            if (int.Parse(TimeSpan.Parse(lastDateDetected.Value).TotalHours.ToString()) > 5)
                                luisStart = DateTime.ParseExact(lastDateDetected.Value, "HH:mm:ss", new CultureInfo("es-ES", true));
                            else if (lastDateDetected.Timex == "PT0.5H")
                                luisDuration = "PT30M";
                            else
                            {
                                String timex = lastDateDetected.Value;
                                timex = "PT" + timex[1] + "H";

                                luisDuration = timex;
                            }
                        else
                            luisStart = DateTime.ParseExact(lastDateDetected.Value, "HH:mm:ss", new CultureInfo("es-ES", true));
                    else if (lastDateDetected.Type == "duration")
                        luisDuration = lastDateDetected.Timex;
                    else if (lastDateDetected.Type == "datetimerange")
                    {
                        if (luisResult.Duration == null)
                        {
                            if (lastDateDetected.Timex[1] != 'X' && DateTime.Parse(lastDateDetected.Start).Hour != 1)
                            {
                                String start = lastDateDetected.Start;
                                luisStart = DateTime.Parse(start);

                                String duration = lastDateDetected.Timex.Replace(")", "").Split(',')[2];
                                luisDuration = recognizeDuration(duration);
                            }
                            else
                                luisStart = DateTime.Parse((TimexResolver.Resolve(new[] { lastDateDetected.Timex.Split(',')[1] }, DateTime.Today)).Values.Last().Value);

                        }
                        else
                        {
                            luisStart = DateTime.Parse((TimexResolver.Resolve(new[] { lastDateDetected.Timex.Split(',')[1] }, DateTime.Today)).Values.Last().Value);
                            luisDuration = recognizeDuration(luisResult.Duration);
                        }
                    }
                }
            }

            if (luisResult.Duration != null)
                luisDuration = recognizeDuration(luisResult.Duration);

            string recognizeDuration(String duration)
            {
                if (duration[0] == 'T')
                    return 'P' + duration + 'H';
                if (duration == "PT0.5H")
                    return "PT30M";
                else
                    return duration;
            }

            BookingAssetDetails bookingAssetDetails = new BookingAssetDetails()
            {
                // Get destination and origin from the composite entities arrays.
                Start = luisDate,
                Duration = luisDuration,
                Activity = userDetails.Activity,
                UserEmail = userDetails.UserEmail,
                UserName = userDetails.UserName,
                TokenWebex = userDetails.TokenWebex,
                Type = BookingAssetDetails.ReservationType.Virtual
            };
            //luisResult.Entities
            switch (userDetails.Activity)
            {
                //START --- BookingAssetGroup
                case AssetsBooking.Intent.CrearReservaVirtual:
                    bookingAssetDetails.Type = BookingAssetDetails.ReservationType.Virtual;
                    return await stepContext.BeginDialogAsync(nameof(BookingAssetDialog), bookingAssetDetails, cancellationToken);
                case AssetsBooking.Intent.CrearReserva:
                    return await stepContext.BeginDialogAsync(nameof(BookingAssetDialog), bookingAssetDetails, cancellationToken);

                //END --- BookingAssetGroup


                //START --- OthersGroup
                case AssetsBooking.Intent.Bienvenido:
                    Dictionary<String, String> values = new Dictionary<String, String>();
                    values.Add("welcomeUser", userDetails.UserName);
                    var welcomeCard = _cardBuilder.getAttachment("welcomeCard", stepContext.Context.Activity.ChannelId, values);

                    await stepContext.Context.SendActivityAsync((Activity)MessageFactory.Attachment(welcomeCard), cancellationToken);
                    break;
                case AssetsBooking.Intent.Despedida:
                    //TODO: despedida hace lo mismo que check out??

                    var getGoodByeMessageText = "Hasta la proxima, que tengas un buen dia";
                    var getWeatherMessage = MessageFactory.Text(getGoodByeMessageText, getGoodByeMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Lo siento, no entendi eso. Intenta preguntar de otra manera";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
                    //END --- OthersGroup
            }

            return await stepContext.NextAsync(false, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, AssetsBooking luisResult, CancellationToken cancellationToken)
        {
            var unsupportedCities = new List<string>();

            if (unsupportedCities.Any())
            {
                var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookDesk
            // the Result here will be null.

            // Restart the main dialog with a different message the second time around
            var promptMessage = "Me alegra poder ayudar 🤖! Si precisas algo mas, no dudes en pedirmelo!";
            if (stepContext.Result is Boolean result && !result)
                return await stepContext.NextAsync(null, cancellationToken);

            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
