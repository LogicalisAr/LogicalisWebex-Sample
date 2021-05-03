// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters.Webex;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SallyBot.Adapters
{
    public class WebexAdapterWithErrorHandler : WebexAdapter
    {
        public WebexAdapterWithErrorHandler(IConfiguration configuration, ILogger<WebexAdapter> logger)
            : base(configuration, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                var errorMessageText = "Hubo un problema al procesar tu respuesta. Podrias enviarla nuevamente o escribir 'cancelar' para terminar esta accion?";
                var errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(errorMessage);
            };
        }
    }
}
