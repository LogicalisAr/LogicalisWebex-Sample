// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using Microsoft.SallyBot;
using System;

namespace SallyBot
{
    public class BookingMeetingDetails : UserDetails
    {
        public DateTime? Start { get; set; }
        public string Duration { get; set; }
        public string TokenWebex { get; set; }
        public string Title { get; set; }

        public string Body { get; set; }

    }
}
