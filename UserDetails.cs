// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using System;

namespace Microsoft.SallyBot
{
    public class UserDetails
    {
        public UserDetails() {
            Activity = AssetsBooking.Intent.None;
        }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string TokenWebex { get; set; }
        public AssetsBooking.Intent Activity { get; set; }

        public AssetsBooking LuisResult { get; set; }
    }
}