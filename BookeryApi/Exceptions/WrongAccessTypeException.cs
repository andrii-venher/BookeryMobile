﻿using System;

namespace BookeryApi.Exceptions
{
    public class WrongAccessTypeException : Exception
    {
        public WrongAccessTypeException() : base("You're not allowed to perform this operation.")
        {
            
        }
    }
}