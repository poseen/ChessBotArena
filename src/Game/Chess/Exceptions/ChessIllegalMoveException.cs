﻿using System;

namespace Game.Chess.Exceptions
{
    public class ChessIllegalMoveException : ChessException
    {
        public ChessIllegalMoveException() : base()
        {
        }

        public ChessIllegalMoveException(string message)
            : base(message)
        {
        }

        public ChessIllegalMoveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
