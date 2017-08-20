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
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Agents
{
    public class AStar : IPathFinder
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PathFinderNodeFast
        {
            public int F; // f = gone + heuristic
            public int G;
            public ushort PX; // Parent
            public ushort PY;
            public byte Status;
        }

        public event PathFinderDebugHandler PathFinderDebug;

        private readonly World _world;
        private readonly PriorityQueue<Point> _open;
        private readonly List<PathNode> _close = new List<PathNode>();
        private bool _stop;
        private bool _stopped = true;
        private int _horizontal;
        private readonly PathFinderNodeFast[,] _calcGrid;
        private byte _openNodeValue = 1;
        private byte _closeNodeValue = 2;

        private int _closeNodeCounter;
        private readonly ushort _gridX;
        private readonly ushort _gridY;
        private bool _found;
        private int _newG;

        public AStar(World world)
        {
            Diagonals = true;
            Formula = HeuristicFormula.Manhattan;
            DebugFoundPath = false;
            DebugProgress = false;
            SearchLimit = 2000;
            TieBreaker = false;
            PunishChangeDirection = false;
            HeuristicEstimate = 2;
            HeavyDiagonals = false;
            
            if (world == null)
                throw new Exception("World cannot be null");

            _world = world;
            _gridX = (ushort)world.Length;
            _gridY = (ushort)world.Length;
            _calcGrid = new PathFinderNodeFast[_gridX, _gridY];
            _open = new PriorityQueue<Point>(new NodeMatrixComparer(_calcGrid));
        }

        public bool Stopped
        {
            get { return _stopped; }
        }
        
        private readonly object _lockable = new object();

        public HeuristicFormula Formula { get; set; }
        public bool Diagonals { get; set; }
        public bool HeavyDiagonals { get; set; }
        public int HeuristicEstimate { get; set; }
        public bool PunishChangeDirection { get; set; }
        public bool TieBreaker { get; set; }
        public int SearchLimit { get; set; }
        public bool DebugProgress { get; set; }
        public bool DebugFoundPath { get; set; }

        public void Stop()
        {
            _stop = true;
        }

        public IEnumerable<PathNode> FindPath(Agent agent, Point start, Point end)
        {
            lock (_lockable)
            {
                _found = false;
                _stop = false;
                _stopped = false;
                _closeNodeCounter = 0;
                _openNodeValue += 2;
                _closeNodeValue += 2;
                _open.Clear();
                _close.Clear();

#if DEBUG
                if (DebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, start.X, start.Y, PathFinderNodeType.Start, -1, -1);
                if (DebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, end.X, end.Y, PathFinderNodeType.End, -1, -1);
#endif

                _calcGrid[start.X, start.Y].G = 0;
                _calcGrid[start.X, start.Y].F = HeuristicEstimate;
                _calcGrid[start.X, start.Y].PX = (ushort)start.X;
                _calcGrid[start.X, start.Y].PY = (ushort)start.Y;
                _calcGrid[start.X, start.Y].Status = _openNodeValue;

                Point location = start;
                _open.Push(location);

                while (_open.Count > 0 && !_stop)
                {
                    location = _open.Pop();

                    if (_calcGrid[location.X, location.Y].Status == _closeNodeValue)
                        continue;

#if DEBUG
                    if (DebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, location.X, location.Y, PathFinderNodeType.Current, -1, -1);
#endif

                    if (location == end)
                    {
                        _calcGrid[location.X, location.Y].Status = _closeNodeValue;
                        _found = true;
                        break;
                    }

                    if (_closeNodeCounter > SearchLimit)
                    {
                        _stopped = true;
                        return null;
                    }

                    if (PunishChangeDirection)
                        _horizontal = (location.X - _calcGrid[location.X, location.Y].PX);

                    for (int i = 0; i < (Diagonals ? 8 : 4); i++)
                    {
                        Move move = Utils.Movements[i];

                        Point newLocation = location + move;

                        if (!_world.CanMove(location, move))
                            continue;

                        _newG = HeavyDiagonals && i > 3
                                    ? _calcGrid[location.X, location.Y].G +
                                      (int)(_world.CostOf(location) * 2.41)
                                    : _calcGrid[location.X, location.Y].G + _world.CostOf(location);

                        if (PunishChangeDirection)
                        {
                            int toAdd = Math.Abs(newLocation.X - end.X) + Math.Abs(newLocation.Y - end.Y);
                            if ((newLocation.X - location.X) != 0 && _horizontal == 0)
                                _newG += toAdd;
                            if ((newLocation.Y - location.Y) != 0 && _horizontal != 0)
                                _newG += toAdd;
                        }

                        var newLocTmp = _calcGrid[newLocation.X, newLocation.Y];
                        if ((newLocTmp.Status == _openNodeValue || newLocTmp.Status == _closeNodeValue) && newLocTmp.G <= _newG)
                            continue;

                        _calcGrid[newLocation.X, newLocation.Y].PX = (ushort)location.X;
                        _calcGrid[newLocation.X, newLocation.Y].PY = (ushort)location.Y;
                        _calcGrid[newLocation.X, newLocation.Y].G = _newG;

                        int h;
                        switch (Formula)
                        {
                            default:
                            case HeuristicFormula.Manhattan:
                                h = HeuristicEstimate * (Math.Abs(newLocation.X - end.X) + Math.Abs(newLocation.Y - end.Y));
                                break;
                            case HeuristicFormula.MaxDxDy:
                                h = HeuristicEstimate * (Math.Max(Math.Abs(newLocation.X - end.X), Math.Abs(newLocation.Y - end.Y)));
                                break;
                            case HeuristicFormula.DiagonalShortCut:
                                {
                                    int diagonal = Math.Min(Math.Abs(newLocation.X - end.X),
                                                            Math.Abs(newLocation.Y - end.Y));
                                    int straight = (Math.Abs(newLocation.X - end.X) + Math.Abs(newLocation.Y - end.Y));
                                    h = (HeuristicEstimate * 2) * diagonal + HeuristicEstimate * (straight - 2 * diagonal);
                                }
                                break;
                            case HeuristicFormula.Euclidean:
                                h = (int)(HeuristicEstimate * Math.Sqrt(Math.Pow((newLocation.Y - end.X), 2) + Math.Pow((newLocation.Y - end.Y), 2)));
                                break;
                            case HeuristicFormula.EuclideanNoSqr:
                                h = (int)(HeuristicEstimate * (Math.Pow((newLocation.X - end.X), 2) + Math.Pow((newLocation.Y - end.Y), 2)));
                                break;
                            case HeuristicFormula.Custom1:
                                {
                                    Point dxy = new Point(Math.Abs(end.X - newLocation.X),
                                                          Math.Abs(end.Y - newLocation.Y));
                                    int orthogonal = Math.Abs(dxy.X - dxy.Y);
                                    int diagonal = Math.Abs(((dxy.X + dxy.Y) - orthogonal) / 2);
                                    h = HeuristicEstimate * (diagonal + orthogonal + dxy.X + dxy.Y);
                                }
                                break;
                        }

                        if (TieBreaker)
                        {
                            int dx1 = location.X - end.X;
                            int dy1 = location.Y - end.Y;
                            int dx2 = start.X - end.X;
                            int dy2 = start.Y - end.Y;
                            int cross = Math.Abs(dx1 * dy2 - dx2 * dy1);
                            h = (int)(h + cross * 0.001);
                        }

                        _calcGrid[newLocation.X, newLocation.Y].F = _newG + h;

#if DEBUG
                        if (DebugProgress && PathFinderDebug != null)
                            PathFinderDebug(location.X, location.Y, newLocation.X, newLocation.Y, PathFinderNodeType.Open,
                                _calcGrid[newLocation.X, newLocation.Y].F, _calcGrid[newLocation.X, newLocation.Y].G);
#endif

                        _open.Push(newLocation);
                        _calcGrid[newLocation.X, newLocation.Y].Status = _openNodeValue;
                    }

                    _closeNodeCounter++;
                    _calcGrid[location.X, location.Y].Status = _closeNodeValue;

#if DEBUG
                    if (DebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, location.X, location.Y, PathFinderNodeType.Close,
                            _calcGrid[location.X, location.Y].F, _calcGrid[location.X, location.Y].G);
#endif
                }

                if (_found)
                {
                    _close.Clear();

                    PathFinderNodeFast fNodeTmp = _calcGrid[end.X, end.Y];
                    PathNode fNode;
                    fNode.F = fNodeTmp.F;
                    fNode.G = fNodeTmp.G;
                    fNode.H = 0;
                    fNode.Px = fNodeTmp.PX;
                    fNode.Py = fNodeTmp.PY;
                    fNode.X = end.X;
                    fNode.Y = end.Y;

                    while (fNode.X != fNode.Px || fNode.Y != fNode.Py)
                    {
                        _close.Add(fNode);
#if DEBUG
                        if (DebugFoundPath && PathFinderDebug != null)
                            PathFinderDebug(fNode.Px, fNode.Py, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
#endif
                        int posX = fNode.Px;
                        int posY = fNode.Py;
                        fNodeTmp = _calcGrid[posX, posY];
                        fNode.F = fNodeTmp.F;
                        fNode.G = fNodeTmp.G;
                        fNode.H = 0;
                        fNode.Px = fNodeTmp.PX;
                        fNode.Py = fNodeTmp.PY;
                        fNode.X = posX;
                        fNode.Y = posY;
                    }

                    _close.Add(fNode);
#if DEBUG
                    if (DebugFoundPath && PathFinderDebug != null)
                        PathFinderDebug(fNode.Px, fNode.Py, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
#endif

                    _stopped = true;
                    return _close;
                }
                _stopped = true;
                return null;
            }
        }

        internal class NodeMatrixComparer : IComparer<Point>
        {
            private readonly PathFinderNodeFast[,] _matrix;

            public NodeMatrixComparer(PathFinderNodeFast[,] matrix)
            {
                _matrix = matrix;
            }

            public int Compare(Point a, Point b)
            {
                if (_matrix[a.X, a.Y].F > _matrix[b.X, b.Y].F)
                    return 1;
                if (_matrix[a.X, a.Y].F < _matrix[b.X, b.Y].F)
                    return -1;
                return 0;
            }
        }
    }
}
