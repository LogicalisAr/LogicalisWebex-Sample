// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.SallyBot
{
    // Extends the partial FlightBooking class with methods and properties that simplify accessing entities in the luis results
    public partial class AssetsBooking
    {

        // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
        // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.
        public string Date
            => Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0];

        // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
        // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.
        public string Start
            => Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault().ToString();

        public string Duration
            => Entities.datetime != null && Entities.datetime.Length > 1 && Entities.datetime?.LastOrDefault().Type == "duration" ? Entities.datetime?.LastOrDefault()?.Expressions.FirstOrDefault().ToString() : null;
        //=> Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0];
    }
}
