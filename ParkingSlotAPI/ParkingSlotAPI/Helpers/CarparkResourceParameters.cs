﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingSlotAPI.Helpers
{
    public class CarparkResourceParameters
    {
        const int maxPageSize = 20;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;

        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = (value > maxPageSize) ? maxPageSize : value;
            }
        }

        public string AgencyType { get; set; }

        public string VehType { get; set; } = "All";

        public DateTime StartDateTime { get; set; } = DateTime.MinValue;

        public DateTime EndDateTime { get; set; } = DateTime.MinValue;

        public string SearchQuery { get; set; }

        public double Price { get; set; } = Double.MaxValue;
    }
}
