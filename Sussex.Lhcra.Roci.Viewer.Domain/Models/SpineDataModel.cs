﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sussex.Lhcra.Roci.Viewer.Domain.Models
{
    public class SpineDataModel
    {
        public bool isValid { get; set; }
        public string SpineData { get; set; }
        public string ErrorMessage { get; set; }
    }
}
