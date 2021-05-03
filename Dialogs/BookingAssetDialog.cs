// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.SallyBot;
using Microsoft.SallyBot.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SallyBot.DTOs;
using SallyBot.Services;
using SallyBot.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SallyBot.Dialogs
{
    public class BookingAssetDialog : CancelAndHelpDialog
    {
        private static readonly String asunto = "Titulo de la reunion:";
        private static readonly String errorAsunto = "Error ⚠️ en el titulo de la reunion";
        private static readonly String body = "Agenda 📑 de la reunion:";
        private static readonly String errorBody = "Error ⚠️ en el detalle de la reunion";
        private readonly WebexMeetingService _webexMeetingService;
        private readonly CardBuilder _cardBuilder;

        public BookingAssetDialog(WebexMeetingService webexMeetingService)
            : base(nameof(BookingAssetDialog))
        {
            _webexMeetingService = webexMeetingService;
            _cardBuilder = new CardBuilder(); ;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new TimeResolverDialog());
            AddDialog(new DurationResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                //ALL
                DateStepAsync,
                TimeStepAsync,
                DurationStepAsync,

                //Room o Virtual
                AsuntoStepAsync,
                BodyStepAsync,

                //ALL
                CreateBookingStepAsync,
                ShowBooking,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> DateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;

            if (bookingDetails.Start == null || bookingDetails.Start.Value.Date < DateTime.Now.Date)
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.Start, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.Start.ToString(), cancellationToken);
        }

        private async Task<DialogTurnResult> TimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;

            bookingDetails.Start = DateTime.Parse(stepContext.Result.ToString());

            if (bookingDetails.Start == null || bookingDetails.Start.Value.Hour < 8 || bookingDetails.Start.Value.Hour >= 19)
            {
                return await stepContext.BeginDialogAsync(nameof(TimeResolverDialog), bookingDetails.Start, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.Start.ToString(), cancellationToken);
        }

        private async Task<DialogTurnResult> DurationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;

            bookingDetails.Start = DateTime.Parse(stepContext.Result.ToString());
            bookingDetails.Start = new DateTime(bookingDetails.Start.Value.Year, bookingDetails.Start.Value.Month, bookingDetails.Start.Value.Day, bookingDetails.Start.Value.Hour, bookingDetails.Start.Value.Minute, 0).AddMinutes(bookingDetails.Start.Value.Minute % 30 == 0 ? 0 : 30 - bookingDetails.Start.Value.Minute % 30);

            if (bookingDetails.Duration == null)
            {
                return await stepContext.BeginDialogAsync(nameof(DurationResolverDialog), bookingDetails.Duration, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.Duration, cancellationToken);
        }

        private async Task<DialogTurnResult> AsuntoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;
            bookingDetails.Duration = (string)stepContext.Result;

            if (bookingDetails.Title == null)
            {
                var promptMessage = MessageFactory.Text(asunto, asunto, InputHints.ExpectingInput);
                var repromptMessage = MessageFactory.Text(errorAsunto, errorAsunto, InputHints.ExpectingInput);

                // We were not given any date at all so prompt the user.
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        RetryPrompt = repromptMessage,
                    }, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> BodyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;
            if (stepContext.Result != null)
            {
                bookingDetails.Title = (string)stepContext.Result;
            }

            if (bookingDetails.Body == null)
            {

                var promptMessage = MessageFactory.Text(body, body, InputHints.ExpectingInput);
                var repromptMessage = MessageFactory.Text(errorBody, errorBody, InputHints.ExpectingInput);

                // We were not given any date at all so prompt the user.
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        RetryPrompt = repromptMessage,
                    }, cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> CreateBookingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;

            if (stepContext.Result != null)
                bookingDetails.Body = (string)stepContext.Result;

            JObject json = new JObject();
            json["title"] = bookingDetails.Title;
            json["agenda"] = bookingDetails.Body;
            json["password"] = Guid.NewGuid().ToString().Substring(0, 5);
            json["start"] = bookingDetails.Start.Value.ToString("yyyy-MM-dd HH:mm:ss");
            json["end"] = bookingDetails.Start.Value.AddMinutes(getDurationInMinutes(bookingDetails.Duration)).ToString("yyyy-MM-dd HH:mm:ss");
            json["timezone"] = "America/Argentina/Buenos_Aires";
            json["enabledAutoRecordMeeting"] = "false";
            json["allowAnyUserToBeCoHost"] = "true";

            WebexMeetingResponseDTO webexMeetingResponseDTO = await _webexMeetingService.createMeeting(bookingDetails.TokenWebex, json);

            if (webexMeetingResponseDTO.message == null)
            {
                return await stepContext.NextAsync(bookingDetails.Id, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Ocurrio un error ⚠️ al crear la reservacion por favor intenta de nuevo en unos minutos ⏳.");
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> ShowBooking(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;
            if (stepContext.Result != null)
            {
                bookingDetails.Id = int.Parse(stepContext.Result.ToString());

                var bookApprovedCard = _cardBuilder.BookApprovedCardAdaptiveCardAttachment(stepContext);
                await stepContext.Context.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));

                return await stepContext.NextAsync(bookingDetails, cancellationToken);
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private Attachment CreateAdaptiveCardAttachment((string text, object value)[] choices, string id, WaterfallStepContext stepContext)
        {
            var choiceItems = new List<dynamic>(choices.Select(choice => new { title = choice.text, choice.value }));
            var cardResourcePath = "SallyBot.Cards.seleccionSalaCard.json";
            if (stepContext.Context.Activity.ChannelId == "webex")
                cardResourcePath = "SallyBot.Cards.seleccionSalaCardWebex.json";
            var serializedChoices = JsonConvert.SerializeObject(choiceItems.ToArray());

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    adaptiveCard = adaptiveCard.Replace("\"$(choices)\"", serializedChoices);
                    adaptiveCard = adaptiveCard.Replace("$(id)", id);
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        private int getDurationInMinutes(string duration)
        {
            TimeSpan meetingTime;
            String timex = duration;

            if (timex[0] == 'T')
                if (timex.Contains(":"))
                    timex = "PT" + timex[1] + timex[2] + "H" + timex[4] + timex[5] + "M";
                else
                    timex = "PT" + timex[1] + timex[2] + "H00M";

            if (timex[0] == 'P')
                meetingTime = XmlConvert.ToTimeSpan(timex);
            else
                meetingTime = TimeSpan.Parse(timex);

            int result = meetingTime.Hours * 60 + meetingTime.Minutes;
            if (result == 0)
            {
                result = int.Parse(duration);
            }

            return result;
        }
    }
}
