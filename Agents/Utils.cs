/*
 *  Visual Agents (for clustering)
 *
 *  Copyright (C) 2014 Juan Carlos Pujol Mainegra
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.  
 *
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Agents
{
    /// <summary>
    /// Possible movement steps (in left-top (0,0) screen coordinates)
    /// </summary>
    public enum Direction : ushort
    {
        StayStill = 0x0000,

        // regulars
        East = 0x0100,
        West = 0xFF00,
        South = 0x0001,
        North = 0x00FF,

        // diagonals
        NorthEast = 0x01FF,
        NorthWest = 0xFFFF,
        SouthEast = 0x0101,
        SouthWest = 0xFF01
    };

    /// <summary>
    /// Possible movement step described
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Move {Dxy}")]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Move
    {
        public Move(Direction direction)
        {
            Dx = Dy = 0;
            Dxy = direction;
        }

        public Move(sbyte dx, sbyte dy)
        {
            Dxy = 0;
            Dx = dx;
            Dy = dy;
        }

        public static readonly Move StayStill = new Move(Direction.StayStill);
        public static readonly Move East = new Move(Direction.East);
        public static readonly Move West = new Move(Direction.West);
        public static readonly Move North = new Move(Direction.North);
        public static readonly Move South = new Move(Direction.South);
        public static readonly Move NorthEast = new Move(Direction.NorthEast);
        public static readonly Move NorthWest = new Move(Direction.NorthWest);
        public static readonly Move SouthEast = new Move(Direction.SouthEast);
        public static readonly Move SouthWest = new Move(Direction.SouthWest);
        
        public static Point operator +(Point location, Move move)
        {
            return new Point((ushort)(location.X + move.Dx), (ushort)(location.Y + move.Dy));
        }

        public static Point operator -(Point location, Move move)
        {
            return new Point((ushort)(location.X - move.Dx), (ushort)(location.Y - move.Dy));
        }

        public static bool operator ==(Move a, Move b)
        {
            return a.Dxy == b.Dxy;
        }

        public static bool operator !=(Move a, Move b)
        {
            return a.Dxy != b.Dxy;
        }

        [FieldOffset(0)]
        public sbyte Dy;
        [FieldOffset(1)]
        public sbyte Dx;
        [FieldOffset(0)]
        public Direction Dxy;
    }

    [System.Diagnostics.DebuggerDisplay("X = {X}, Y = {Y}")]
    public struct PathNode
    {
        public PathNode(Point p)
        {
            F = G = H = Px = Py = 0;
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// Total cost. F = gone + heuristic
        /// </summary> 
        public int F;
        /// <summary>
        /// Gone (real cost)
        /// </summary> 
        public int G;
        /// <summary>
        /// Heuristic cost
        /// </summary> 
        public int H;
        /// <summary>
        /// This X location
        /// </summary> 
        public int X;
        /// <summary>
        /// This Y location
        /// </summary> 
        public int Y;
        /// <summary>
        /// Parent X location
        /// </summary> 
        public int Px;
        /// <summary>
        /// Parent Y location
        /// </summary> 
        public int Py;
    }

    public enum PathFinderNodeType : byte
    {
        Start = 1,
        End = 2,
        Open = 4,
        Close = 8,
        Current = 16,
        Path = 32
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PathFinderNodeInfo
    {
        public PathFinderNodeType PathFinderNodeType;
        public byte Team;
    }

    /// <summary>
    /// Formula for calculating paths
    /// </summary>
    public enum HeuristicFormula
    {
        Manhattan = 1,
        MaxDxDy = 2,
        DiagonalShortCut = 3,
        Euclidean = 4,
        EuclideanNoSqr = 5,
        Custom1 = 6
    }

    public delegate void PathFinderDebugHandler(int fromX, int fromY, int x, int y, PathFinderNodeType type, int totalCost, int cost);

    public static class Utils
    {
        /// <summary>
        /// Direction of movement (first four (0-3) ones are for agents). Last ones (4-7) are diagonals.
        /// </summary>
        public static readonly Direction[] Directions = new[] {
            Direction.East, Direction.West, Direction.North, Direction.South,
            Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest
        };

        /// <summary>
        /// Movements (first four (0-3) ones are for agents). Last ones (4-7) are diagonals.
        /// </summary>
        public static readonly Move[] Movements = new[] {
            Move.East, Move.West, Move.North, Move.South,
            Move.NorthEast, Move.NorthWest, Move.SouthEast, Move.SouthWest
        };

        private static Random _rnd;

        /// <summary>
        /// Shuffling algorithm.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static T[] FisherYatesInsideOutPermutation<T>(IList<T> source, int? seed = null)
        {
            T[] result = new T[source.Count];
            _rnd = _rnd ?? (seed != null ? new Random(seed.Value) : new Random());

            for (int i = source.Count - 1; i > 0; i--)
            {
                int j = _rnd.Next(0, i + 1);
                if (j != i)
                    result[i] = result[j];
                result[j] = source[i];
            }

            return result;
        }

        /// <summary>
        /// Shuffling algorithm.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="source"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static U FisherYatesInsideOutPermutation<T, U>(U source, int? seed = null)
            where U : IList<T>, new()
        {
            U result = new U();
            for (int i = 0; i < source.Count; i++)
                result.Add(default(T));

            _rnd = _rnd ?? (seed != null ? new Random(seed.Value) : new Random());

            for (int i = source.Count - 1; i > 0; i--)
            {
                int j = _rnd.Next(0, i + 1);
                if (j != i)
                    result[i] = result[j];
                result[j] = source[i];
            }

            return result;
        }

        /// <summary>
        /// In-place array shuffling algorithm.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="seed"></param>
        public static void FisherYatesPermutation<T>(IList<T> array, int? seed = null)
        {
            _rnd = _rnd ?? (seed != null ? new Random(seed.Value) : new Random());

            for (int i = array.Count - 1; i > 0; i--)
            {
                int j = _rnd.Next(0, i + 1);
                // swap them
                T tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }
    }
}
