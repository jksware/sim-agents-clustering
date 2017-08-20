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
using System.Linq;

namespace Agents
{
    public abstract class Agent : BoardObject
    {
        protected Agent(World world, Team team, Point position)
            : base(world, team, position)
        {
        }

        public abstract Move ComputeNextMove();

        public IEnumerable<PathNode> Path;

        public static IEnumerable<Move> Steps(IEnumerable<PathNode> positions)
        {
            var p = positions as PathNode[] ?? positions.ToArray();
            for (int i = 1; i < p.Length; i++)
                yield return new Move((sbyte) (p[i].X - p[i - 1].X), (sbyte) (p[i].Y - p[i - 1].Y));
        }

        public static IEnumerable<Tuple<Point, Move>> Corners(IEnumerable<PathNode> positions)
        {
            var positionsEnumerator = positions.GetEnumerator();
            var movesEnumerator = Steps(positions).GetEnumerator();

            var lastMove = Move.StayStill;//movesEnumerator.Current;

            while (movesEnumerator.MoveNext() && positionsEnumerator.MoveNext())
            {
                if (lastMove != movesEnumerator.Current)
                {
                    var point = new Point(positionsEnumerator.Current.X, positionsEnumerator.Current.Y);
                    var move = movesEnumerator.Current;
                    yield return new Tuple<Point, Move>(point, move);
                }
                lastMove = movesEnumerator.Current;
            }

            var lastPosition = new Point(positionsEnumerator.Current.X, positionsEnumerator.Current.Y);
            yield return new Tuple<Point, Move>(lastPosition, lastMove);
        }

        public bool IsNextTo(Point other, bool includeDiagonal)
        {
            var absX = Math.Abs(Position.X - other.X);
            var absY = Math.Abs(Position.Y - other.Y);
            return absX <= 1 && absY == 0 || absY <= 1 && absX == 0 || includeDiagonal && absX <= 1 && absY <= 1;
        }
    }
}