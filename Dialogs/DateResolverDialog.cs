// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SallyBot.Dialogs
{
    public class DateResolverDialog : CancelAndHelpDialog
    {
        private const string PromptMsgText = "Cuando quieres reservarlo?";
        private const string PromptMsgText_NotAvailable = "En que fecha 📅 no estaras disponible?";
        private const string PromptMsgText_Update = "Cual sera la nueva fecha 📅?";
        private const string RepromptMsgText = "Lo siento, ingrese una fecha correcta 📅.";

        private DateTime dateTime;
        private AssetsBooking.Intent _intent;

        public DateResolverDialog(AssetsBooking.Intent intent = AssetsBooking.Intent.None, string id = null)
            : base(id ?? nameof(DateResolverDialog))
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

            if (timex == null || timex.Length.Equals(0))
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
            if (!timexProperty.Types.Contains(Constants.TimexTypes.Definite))
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
            var timex = ((List<DateTimeResolution>)stepContext.Result)[((List<DateTimeResolution>)stepContext.Result).Count - 1];
            IFormatProvider culture = new CultureInfo("es-ES", true);

            if (timex.Timex == "PRESENT_REF")
            {
                dateTime = DateTime.Now;
            }
            else
            {
                dateTime = DateTime.ParseExact(timex.Value, "yyyy-MM-dd", culture);
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

                    var timex = promptContext.Recognized.Value[promptContext.Recognized.Value.Count - 1].Value.Split(' ')[0];

                    DateTime date = DateTime.ParseExact(timex, "yyyy-MM-dd", new CultureInfo("es-ES", true));

                    // If this is a definite Date including year, month and day we are good otherwise reprompt.
                    // A better solution might be to let the user know what part is actually missing.

                    isDefinite = TimexProperty.FromDate(date).Types.Contains(Constants.TimexTypes.Date);
                    if (!isDefinite)
                        isDefinite = new TimexProperty(timex).Types.Contains(Constants.TimexTypes.Definite);
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
