using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.SallyBot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;

namespace SallyBot.Utils
{
    public class CardBuilder
    {
        private static String DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";
        private static String SHORT_DATE_FORMAT = "dddd dd 'de' MMMM";
        private static String SHORT_TIME_FORMAT = "HH:mm";

        public Attachment BookApprovedCardAdaptiveCardAttachment(WaterfallStepContext stepContext)
        {
            var bookingDetails = (BookingAssetDetails)stepContext.Options;

            string cardName = "bookVirtualApprovedCard";
            DateTime checkout = bookingDetails.Start.Value.AddMinutes(this.getDurationInMinutes(bookingDetails.Duration));
            string duration = String.Format("{0}-{1}", bookingDetails.Start?.ToString(SHORT_TIME_FORMAT), checkout.ToString(SHORT_TIME_FORMAT));
            string date = bookingDetails.Start?.ToString(SHORT_DATE_FORMAT, new CultureInfo("es-ES"));

            Dictionary<String, String> values = new Dictionary<String, String>();
            values.Add("checkin", bookingDetails.Start?.ToString(DATE_TIME_FORMAT));
            values.Add("checkout", (checkout).ToString(DATE_TIME_FORMAT));
            values.Add("date", char.ToUpper(date.First()) + date.Substring(1).ToLower());
            values.Add("hours", duration);
            values.Add("userName", bookingDetails.UserEmail);
            values.Add("subject", bookingDetails.Title);
            values.Add("body", bookingDetails.Body);

            values.Add("whereVirtual", "Reunion de Webex");

            return getAttachment(cardName, stepContext.Context.Activity.ChannelId, values);
        }
        public Attachment getAttachment(string path, String channelId, Dictionary<String, String> values)
        {
            var channelName = channelId == "webex" ? "Webex" : "";
            var cardResourcePath = String.Format("SallyBot.Cards.{0}{1}.json", path, channelName);

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    if (values != null)
                    {
                        foreach (KeyValuePair<String, String> kv in values)
                            adaptiveCard = replaceValue(adaptiveCard, kv.Key, kv.Value, channelId);
                    }

                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        private String replaceValue(string card, string code, String value, string channelId)
        {
            if (channelId == "webex" && code != "link" && value != null)
            {
                value = WebUtility.HtmlEncode(value.ToString());
            }
            if (code == "multiselect")
            {
                return card.Replace("\"$(" + code + ")\"", value);
            }
            else if (code == "welcomeUser")
            {
                return card.Replace("$(" + code + ")", value);
            }
            return card.Replace("\"$(" + code + ")\"", JsonConvert.SerializeObject(value));
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
