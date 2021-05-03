// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SallyBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.SallyBot;

namespace SallyBot.Dialogs
{
    public class DurationResolverDialog : CancelAndHelpDialog
    {
        private const string PromptMsgText = "Cual va a ser la duracion? (Formato: HH:mm)";
        private const string PromptMsgText_NotAvailable = "Por cuanto tiempo no estaras disponible ⛔? (Formato: HH:mm)";
        private const string PromptMsgText_Update = "Cual sera la nueva duracion de la reservacion? (Formato: HH:mm)";
        private const string RepromptMsgText = "No entiendo ❓. 🤷 Por favor, ingresa una duracion.";

        private AssetsBooking.Intent _intent;
        public DurationResolverDialog(AssetsBooking.Intent intent = AssetsBooking.Intent.None, string id = null)
            : base(id ?? nameof(DurationResolverDialog))
        {
            _intent = intent;
            AddDialog(new DateTimePrompt(nameof(DateTimePrompt), DateTimePromptValidator));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timex = (string)stepContext.Options;

            var promptMessage = MessageFactory.Text(PromptMsgText, PromptMsgText, InputHints.ExpectingInput);
            var repromptMessage = MessageFactory.Text(RepromptMsgText, RepromptMsgText, InputHints.ExpectingInput);

            if (timex == null)
            {
                // We were not given any date at all so prompt the user.
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        RetryPrompt = repromptMessage,
                    }, cancellationToken);
            }

            // We have a Date we just need to check it is unambiguous.
            var timexProperty = new TimexProperty(timex);
            if (!timexProperty.Types.Contains(Constants.TimexTypes.Duration))
            {
                // This is essentially a "reprompt" of the data we were given up front.
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = repromptMessage,
                    }, cancellationToken);
            }

            return await stepContext.NextAsync(new List<DateTimeResolution> { new DateTimeResolution { Timex = timex } }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string timex;
            if (((List<DateTimeResolution>)stepContext.Result)[0].Value != null)
            {
                if (((List<DateTimeResolution>)stepContext.Result)[0].Value.Contains(":"))
                {
                    timex = "T" + ((List<DateTimeResolution>)stepContext.Result)[0].Value;
                }
                else
                {
                    int timespan = Convert.ToInt32(((List<DateTimeResolution>)stepContext.Result)[0].Value);
                    timex = "T" + Convert.ToString(TimeSpan.FromSeconds(timespan));
                }
            }
            else
            {
                timex = ((List<DateTimeResolution>)stepContext.Result)[0].Timex;
            }

            return await stepContext.EndDialogAsync(timex, cancellationToken);
        }

        private static Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            Boolean isDefinite = false;

            if (promptContext.Recognized.Succeeded)
            {
                try
                {
                    if (promptContext.Context.Activity.Text.Contains("y media"))
                    {
                        promptContext.Recognized.Value[0].Value = Convert.ToString(Convert.ToInt32(promptContext.Recognized.Value[0].Value) + 1800);
                        promptContext.Recognized.Value[0].Timex = promptContext.Recognized.Value[0].Timex + "30M";
                    }
                    // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
                    // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.

                    string timex = null;
                    if (promptContext.Recognized.Value[0].Value != null)
                    {
                        if (promptContext.Recognized.Value[0].Value.Contains(":"))
                        {
                            timex = "T" + promptContext.Recognized.Value[0].Value;
                        }
                        else
                        {
                            int timespan = Convert.ToInt32(promptContext.Recognized.Value[0].Value);
                            timex = "T" + Convert.ToString(TimeSpan.FromSeconds(timespan));
                        }
                    }
                    else
                    {
                        timex = promptContext.Recognized.Value[0].Timex;
                    }


                    TimexProperty aux = new TimexProperty();
                    TimexParsing.ParseString(timex, aux);

                    int hours = 0;
                    int minutes = 0;
                    int seconds = 0;
                    if (aux.Hours != null)
                        hours = Convert.ToInt32(aux.Hours);
                    else if (aux.Hour != null)
                        hours = Convert.ToInt32(aux.Hour);

                    if (aux.Minutes != null)
                        minutes = Convert.ToInt32(aux.Minutes);
                    else if (aux.Minute != null)
                        minutes = Convert.ToInt32(aux.Minute);

                    if (aux.Seconds != null)
                        seconds = Convert.ToInt32(aux.Seconds);
                    else if (aux.Second != null)
                        seconds = Convert.ToInt32(aux.Second);

                    Time time = new Time(hours, minutes, seconds);

                    // If this is a definite Date including year, month and day we are good otherwise reprompt.
                    // A better solution might be to let the user know what part is actually missing.
                    if (time != null)
                        isDefinite = TimexProperty.FromTime(time).Types.Contains(Constants.TimexTypes.Time);
                }
                catch (Exception e)
                {
                    isDefinite = false;
                    Debug.WriteLine(e.Message);
                }
            }

            return Task.FromResult(isDefinite);
        }
    }
}
