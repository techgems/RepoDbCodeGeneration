﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationRoslynTest.Exceptions
{
    public class DatabaseScanningException : Exception
    {
        public DatabaseScanningException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
