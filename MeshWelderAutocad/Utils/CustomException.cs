﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Utils
{
    public class CustomException : Exception
    {
        public CustomException(string message) : base(message) { }
        public override string ToString()
        {
            return base.Message;
        }
    }
}
