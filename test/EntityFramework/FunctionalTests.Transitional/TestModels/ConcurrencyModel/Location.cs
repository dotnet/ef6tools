// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace ConcurrencyModel
{
    using System.ComponentModel.DataAnnotations;

    public class Location
    {
        [ConcurrencyCheck]
        public double Latitude { get; set; }

        [ConcurrencyCheck]
        public double Longitude { get; set; }
    }
}
