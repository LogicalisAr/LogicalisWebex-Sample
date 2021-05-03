// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using SallyBot;

namespace Microsoft.SallyBot
{
    public class BookingAssetDetails : BookingMeetingDetails
    {
        public enum ReservationType
        {
            Virtual,
            Cleaning,
            None
        };

        public ReservationType Type { get; set; }
        public int Id { get; set; }

    }
}