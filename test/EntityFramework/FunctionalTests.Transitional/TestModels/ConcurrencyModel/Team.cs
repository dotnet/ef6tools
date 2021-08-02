﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace ConcurrencyModel
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;

    public class Team
    {
        private readonly ObservableCollection<Driver> _drivers = new ObservableListSource<Driver>();
        private readonly ObservableCollection<Sponsor> _sponsors = new ObservableCollection<Sponsor>();

        [Timestamp]
        public byte[] Version { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Constructor { get; set; }
        public string Tire { get; set; }
        public string Principal { get; set; }
        public int ConstructorsChampionships { get; set; }
        public int DriversChampionships { get; set; }
        public int Races { get; set; }
        public int Victories { get; set; }
        public int Poles { get; set; }
        public int FastestLaps { get; set; }

        public virtual Engine Engine { get; set; } // Independent Association

        public virtual Chassis Chassis { get; set; }

        public virtual ICollection<Driver> Drivers
        {
            get { return _drivers; }
        }

        public virtual ICollection<Sponsor> Sponsors
        {
            get { return _sponsors; }
        }

        public int? GearboxId { get; set; }
        public virtual Gearbox Gearbox { get; set; } // Uni-directional

        public const int McLaren = 1;
        public const int Mercedes = 2;
        public const int RedBull = 3;
        public const int Ferrari = 4;
        public const int Williams = 5;
        public const int Renault = 6;
        public const int ForceIndia = 7;
        public const int ToroRosso = 8;
        public const int Lotus = 9;
        public const int Hispania = 10;
        public const int Sauber = 11;
        public const int Virgin = 12;
    }
}
