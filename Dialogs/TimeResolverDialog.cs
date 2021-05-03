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
    public class TimeResolverDialog : CancelAndHelpDialog
    {
        private const string PromptMsgText = "Desde que horario ⏱️ te gustaria hacer la reservacion? (Formato: HH:mm)";
        private const string PromptMsgText_NotAvailable = "En que horario ⏱️ no estaras disponible ⛔? (Formato: HH:mm)";
        private const string PromptMsgText_Update = "Cual sera el nuevo horario de la reserva? (Formato: HH:mm)";
        private const string RepromptMsgText = "No te entiendo ❓. 🤷 Por favor, ingresá un horario ⏱️ valido con el formato HH:mm.";

        private DateTime dateTime;
        private AssetsBooking.Intent _intent;

        public TimeResolverDialog(AssetsBooking.Intent intent = AssetsBooking.Intent.None, string id = null)
            : base(id ?? nameof(TimeResolverDialog))
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
            dateTime = (DateTime)stepContext.Options;

            var promptMessage = MessageFactory.Text(PromptMsgText, PromptMsgText, InputHints.ExpectingInput);
            var repromptMessage = MessageFactory.Text(RepromptMsgText, RepromptMsgText, InputHints.ExpectingInput);

            // We were not given any date at all so prompt the user.
            return await stepContext.PromptAsync(nameof(DateTimePrompt),
                new PromptOptions
                {
                    Prompt = promptMessage,
                    RetryPrompt = repromptMessage,
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timex = ((List<DateTimeResolution>)stepContext.Result)[0];

            TimeSpan ts;
            if (timex.Timex == "PRESENT_REF")
            {
                dateTime = DateTime.Now;
            }
            else
            {
                if (timex.Value.Contains(":"))
                    ts = TimeSpan.Parse(timex.Value);
                else
                {
                    int timespan = Convert.ToInt32(timex.Value);
                    ts = TimeSpan.FromSeconds(timespan);
                }
                dateTime = dateTime.Date + ts;
            }

            return await stepContext.EndDialogAsync(dateTime, cancellationToken);
        }

        private static Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            Boolean isDefinite = false;

            if (promptContext.Recognized.Succeeded)
            {
                try
                {
                    // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
                    // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.

                    var timex = promptContext.Recognized.Value[0];

                    if (timex != null)
                    {
                        TimeSpan ts = DateTime.Now.TimeOfDay;
                        if (timex.Timex != "PRESENT_REF")
                        {
                            if (timex.Value.Contains(":"))
                                ts = TimeSpan.Parse(timex.Value);
                            else
                            {
                                int timespan = Convert.ToInt32(timex.Value);
                                ts = TimeSpan.FromSeconds(timespan);
                            }
                        }

                        Time time = new Time(Convert.ToInt32(ts.Hours), Convert.ToInt32(ts.Minutes), Convert.ToInt32(ts.Seconds));

                        if (time.Hour >= 8 && time.Hour < 19)
                        {
                            // If this is a definite Date including year, month and day we are good otherwise reprompt.
                            // A better solution might be to let the user know what part is actually missing.
                            isDefinite = TimexProperty.FromTime(time).Types.Contains(Constants.TimexTypes.Time);
                        }
                    }
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
