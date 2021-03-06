﻿using System;
using BoardGame.Game.Abstraction;

namespace BoardGame.Game.Chess
{
    /// <summary>
    /// Represents a position in the chess board.
    /// </summary>
    [Serializable]
    public sealed class Position : IEquatable<Position>
    {
        /// <summary>
        /// Initializes a new instance of the Position structure.
        /// </summary>
        /// <param name="column">The algebraic notation of the column. (A-H)</param>
        /// <param name="row">The algebraic (1-based) notation of the row. (1-8)</param>
        public Position(char column, int row)
        {
            column = char.ToUpperInvariant(column);

            if (column < 'A' || column > 'H')
            {
                throw new ArgumentOutOfRangeException(nameof(Column), "The id of the column has to be between 'A' and 'H'.");
            }

            if (row < 1 || row > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(Column), "The number of the column has to be between 1 and 8.");
            }

            Column = column;
            Row = row;
        }

        /// <summary>
        /// Gets the column's algebraic notation.
        /// </summary>
        public char Column { get; }

        /// <summary>
        /// Gets the row's algebraic (1-based) notation.
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// Gets a value indicating whether the position's colour is black or white in the chessboard.
        /// </summary>
        public bool BlackField => ((Row-1) + (Column-'A')) % 2 == 0;

        #region Operators

        /// <summary>
        /// Compares whether two positions are considered equal.
        /// </summary>
        /// <param name="x">One of the positions.</param>
        /// <param name="y">The other position.</param>
        /// <returns>True if the two positions are considered equal (by the position they represent.) Otherwise false.</returns>
        public static bool operator ==(Position x, Position y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Equals(y);
        }

        /// <summary>
        /// Compares whether two positions are considered different.
        /// </summary>
        /// <param name="x">One of the positions.</param>
        /// <param name="y">The other position.</param>
        /// <returns>True if the two positions are considered different (they represent different positions on the chessboard.) Otherwise false.</returns>
        public static bool operator !=(Position x, Position y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Creates a position from it's algebraic notation.
        /// </summary>
        /// <param name="algebraicNotation">The position's algebraic notation.</param>
        public static explicit operator Position(string algebraicNotation)
        {
            if (algebraicNotation.Length != 2)
            {
                throw new ArgumentException("Algebraic position notation has to be 2 characters long.", nameof(algebraicNotation));
            }

            var col = algebraicNotation[0];
            int row;

            if (!int.TryParse(algebraicNotation[1].ToString(), out row))
            {
                throw new ArgumentException("Algebraic notation's second character has to be an integer.", nameof(algebraicNotation));
            }

            return new Position(col, row);
        }

        /// <summary>
        /// Creates a position from it's index in the array. Array starts from A8 and end with H1.
        /// </summary>
        /// <param name="index">The index of the position in the underlying array.</param>
        public static explicit operator Position(int index)
        {
            if (index < 0 || index > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var col = (char)(index % 8 + 'A');
            var row = 8 - (index / 8);
            return new Position(col, row);
        }

        /// <summary>
        /// Gets the array index of the given position.
        /// </summary>
        /// <param name="position">The position.</param>
        public static explicit operator int(Position position)
        {
            var col = char.ToUpperInvariant(position.Column);
            var row = position.Row;

            if (col < 'A' || col > 'H' || row < 1 || row > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(col));
            }

            var idx = (8 - row) * 8 + (col - 'A');

            return idx;
        }

        /// <summary>
        /// Returns the algebraic representation of the position.
        /// </summary>
        /// <param name="position">The position.</param>
        public static explicit operator string(Position position)
        {
            return $"{position.Column}{position.Row}";
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Returns the algebraic representation of the position.
        /// </summary>
        /// <returns>Algebraic representation of the position. Example: "B2".</returns>
        public override string ToString()
        {
            return (string)this;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Constants.HashBase;
                hash = (Constants.HashXor ^ hash) ^ Column.GetHashCode();
                hash = (Constants.HashXor ^ hash) ^ Row.GetHashCode();
                return hash;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as Position;

            if(obj == null)
            {
                return false;
            }

            return Equals(other);
        }

        public bool Equals(Position other)
        {
            if (other == null)
            {
                return false;
            }

            return Row.Equals(other.Row) && Column.Equals(other.Column);
        }

        #endregion
    }
}
