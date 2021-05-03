# Sally - LogicalisWebexSample

Sally bot allows you to manage your agenda been able to create Webex Meetings and reserve a room in a matter of seconds through simple dialogs from Webex Teams.

To see a live sample you have to add [LogicalisWebexSample@webex.bot](LogicalisWebexSample@webex.bot) on Webex. You need to have a Logicalis or a Cisco user to can use it.

- Use Cases availables: 
  - To create a new Webex Meeting write: "Crear reservacion"

This bot has been created using [Bot Framework](https://dev.botframework.com), it shows how to:

- Use [LUIS](https://www.luis.ai) to implement core AI capabilities
- Implement a multi-turn conversation using Dialogs
- Handle user interruptions for such things as `Help` or `Cancel`
- Prompt for and validate requests for information from the user

## Prerequisites

This sample **requires** prerequisites in order to run.

### Overview

This bot uses [LUIS](https://www.luis.ai), an AI based cognitive service, to implement language understanding.

### Install .NET Core CLI

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 3.1

  ```bash
  # determine dotnet version
  dotnet --version
  ```

### Create a LUIS Application to enable language understanding

The LUIS model for this example can be found under `CognitiveModels/DeskBooking.json` and the LUIS language model setup, training, and application configuration steps can be found [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-luis?view=azure-bot-service-4.0&tabs=cs).

Once you created the LUIS model, update `appsettings.json` with your `LuisAppId`, `LuisAPIKey` and `LuisAPIHostName`.

```json
  "LuisAppId": "Your LUIS App Id",
  "LuisAPIKey": "Your LUIS Subscription key here",
  "LuisAPIHostName": "Your LUIS App region here (i.e: westus.api.cognitive.microsoft.com)"
```
## Create an [Azure Database for PostgreSQL](https://azure.microsoft.com/en-us/services/postgresql/)
Once you created the PostgreSQL service, update `appsettings.json` with your `ConnectionStrings` and then run:
	EntityFrameworkCore\Update-Database -Verbose
It will generate the database.

## Create a Cisco Webex Bots
[Cisco Webex Bots](https://developer.webex.com/docs/bots)
Once you created the Webex Bot, update `appsettings.json` with your `WebexAccessToken`, `WebexSecret` and `WebexWebhookName`.
And then configure the [Cisco Webex Webhooks](https://developer.webex.com/docs/api/v1/webhooks) with the botmiddleware app service url.

## Create a Cisco Webex Integration
[Webex Integration](https://developer.webex.com/docs/integrations)
Once you created the Webex Integration, update `appsettings.json` with your `WebexAPIClientID`, `WebexAPISecretID` and `WebexAccessTokenRedirectURL`.

Note: This integration also use https://developer.webex.com/docs/api/v1/meetings to manage Cisco Webex Meetings through the assistant.

## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.3.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:3978/api/messages`

## Deploy the bot to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.
