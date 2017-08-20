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
    public class ClusterAgent : Agent
    {
        public ClusterAgent(World world, Team team, Point position)
            : base(world, team, position)
        {
        }

        public enum AgentStatus
        {
            /// <summary> 
            // Searching for a gold piece (going to End)
            /// </summary> 
            Seeking = 0,
            /// <summary>
            /// Doing absolutely nothing
            /// </summary> 
            Idle = 1,
            /// <summary> 
            /// Pushing a gold piece towards the base (going to Base)
            /// </summary> 
            Pushing = 2,
            /// <summary> 
            /// Readjusting to continue pushing a piece
            /// </summary>
            Readjusting = 3
        }

        public void RestartAStar()
        {
            _aStar = new AStar(this.World)
            {
                DebugProgress = true,
                DebugFoundPath = true,
                Diagonals = false,
                HeuristicEstimate = 2
            };

            _aStar.PathFinderDebug += this.World.PathFinderDebug;
        }

        public Point SeekPiecePosition;

        public AgentStatus LastStatus;
        private AgentStatus _status;
        public AgentStatus Status
        {
            get { return _status; }
            set
            {
                LastStatus = _status;
                _status = value;
            }
        }
        public Cluster TeamCluster;
        public Point Base;
        public Point End;

        /// <summary>
        /// Path of the gold piece back to the base.
        /// </summary>
        public IEnumerable<PathNode> PathGoldBack;

        public IEnumerator<Tuple<Point, Move>> CornersBack;

        public IEnumerator<Move> StepEnumerator;

        private AStar _aStar;

        public readonly PriorityQueue<Cluster> Clusters = new PriorityQueue<Cluster>(new Cluster.SizeComparer());

        private void SetNullSeek()
        {
            Path = null;
            StepEnumerator = new List<Move>().GetEnumerator();
        }

        protected void SetSeek()
        {
            RestartAStar();
            _aStar.PunishChangeDirection = false;

            // todo get a look on what's happening when one is a movement away
            var seekPos = Point.Empty;
            int i;
            for (i = 0; i < 4; i++)
            {
                seekPos = SeekPiecePosition + Utils.Movements[i];
                if (this.World.CanMove(seekPos))
                    break;
            }
            if (i == 4)
            {
                SetNullSeek();
                return;
            }

            var pathFind = _aStar.FindPath(this, Position, seekPos);

            // if there is no path
            // todo try to get next one (although an ultimately improbable case)
            if (pathFind == null)
            {
                SetNullSeek();
                return;
            }

            Path = pathFind.Reverse();
            StepEnumerator = Steps(Path).GetEnumerator();
        }

        private bool IsGoldInBase(Point location)
        {
            foreach (var move in Utils.Movements)
            {
                var newLoc = location + move;
                if (!World.Exists(newLoc))
                    continue;

                var gold = World[newLoc] as Gold;
                if (gold != null && gold.Cluster == TeamCluster)
                    return true;
            }
            return false;
        }

        public override Move ComputeNextMove()
        {
            // allows the reevaluation of the current status
            bool reeval;
            do
            {
                reeval = false;
                switch (Status)
                {
                    case AgentStatus.Idle:
                        return Move.StayStill;

                    case AgentStatus.Seeking:

                        // allows the set-up of the seeking status (where is it goin'?)
                        if (StepEnumerator == null)
                        {
                            if (Clusters.Peek().Positions.Count == 0)
                                Clusters.Pop();

                            var cluster = Clusters.Peek();
                            if (cluster == null)
                            {
                                // nothing else to-do
                                Status = AgentStatus.Idle;
                                reeval = true;
                                continue;
                            }

                            SeekPiecePosition = cluster.Positions.Last.Value;
                            cluster.Positions.RemoveLast();
                            SetSeek();
                        }

                        if (IsNextTo(SeekPiecePosition, false))// && LastStatus != AgentStatus.Readjusting)
                        {
                            Status = AgentStatus.Pushing;
                            StepEnumerator = null;
                            reeval = true;
                            continue;
                        }

                        if (StepEnumerator.MoveNext())
                        {
                            var nextMove = StepEnumerator.Current;
                            var newPosition = Position + nextMove;

                            // there is something on the way (that was not there when we traced the route)
                            if (!World.CanMove(newPosition))
                            {
                                SetSeek();
                                reeval = true;
                                continue;
                            }

                            return nextMove;
                        }

                        break;

                    case AgentStatus.Pushing:
                        if (World[SeekPiecePosition] is Gold && IsGoldInBase(SeekPiecePosition))
                        {
                            var newGold = (Gold)World[SeekPiecePosition];
                            newGold.Cluster = TeamCluster;
                            TeamCluster.Positions.AddLast(newGold.Position);
                            Status = AgentStatus.Seeking;
                            CornersBack = null;
                            StepEnumerator = null;
                            reeval = true;
                            continue;
                        }

                        if (IsNextTo(Base, false))
                        {
                            Status = AgentStatus.Seeking;
                            StepEnumerator = null;
                            reeval = true;
                            continue;
                        }

                        if (StepEnumerator == null)
                        {
                            if (CornersBack == null)
                            {
                                Status = AgentStatus.Readjusting;
                                reeval = true;
                                continue;
                            }

                            // todo look at this error
                            // it is an off-by-one positioning error, non-recuperable once it happens during a session

                            StepEnumerator = Enumerable.Repeat(CornersBack.Current.Item2, 1).GetEnumerator();
                        }

                        if (StepEnumerator != null && StepEnumerator.MoveNext())
                        {
                            var nextMove = StepEnumerator.Current;
                            var newPosition = Position + nextMove;
                            var newPiecePosition = newPosition + nextMove;
   
                            if (!World.CanMove(newPiecePosition))
                            {
                                Status = AgentStatus.Readjusting;
                                reeval = true;
                                continue;
                            }

                            SeekPiecePosition = newPiecePosition;
                            return nextMove;
                        }

                        Status = AgentStatus.Readjusting;
                        reeval = true;
                        break;

                    case AgentStatus.Readjusting:
                        if (CornersBack == null)
                        {
                            RestartAStar();

                            _aStar.PunishChangeDirection = true;

                            // the last position of the base is next to an empty one
                            Base = TeamCluster.Positions.Last();
                            var moveShuffle = new Move[Utils.Movements.Length];
                            Utils.Movements.CopyTo(moveShuffle, 0);
                            Utils.FisherYatesPermutation(moveShuffle);

                            foreach (var move in moveShuffle)
                            {
                                var temptativePos = Base + move;
                                if (this.World.CanMove(temptativePos))
                                {
                                    Base = temptativePos;
                                    break;
                                }
                            }

                            PathGoldBack = _aStar.FindPath(this, SeekPiecePosition, Base);

                            if (PathGoldBack == null)
                                throw new InvalidOperationException("This is not OK");

                            PathGoldBack = PathGoldBack.Reverse();
                            CornersBack = Corners(PathGoldBack).GetEnumerator();
                            StepEnumerator = null;
                        }

                        if (End == Position)
                        {
                            if (!CornersBack.MoveNext())
                            {
                                //throw new InvalidOperationException();
                                StepEnumerator = null;
                                Status = AgentStatus.Seeking;
                                reeval = true;
                                continue;
                            }

                            var nextPush = CornersBack.Current.Item1;
                            var path = _aStar.FindPath(this, SeekPiecePosition, nextPush);

                            StepEnumerator = Steps(path.Reverse()).GetEnumerator();

                            reeval = true;
                            Status = AgentStatus.Pushing;
                            continue;
                        }

                        if (StepEnumerator == null)
                        {
                            if (CornersBack.Current == null)
                                CornersBack.MoveNext();

                            var cornerPos = CornersBack.Current.Item1;
                            var cornerMove = CornersBack.Current.Item2;

                            // the agent must be set against the direction of movement
                            var sidePos = cornerPos - cornerMove;

                            if (!this.World.CanMove(sidePos) && sidePos != Position)
                            {
                                //CornersBack = null;

                                // may be should leave it there instead
                                Status = AgentStatus.Seeking;
                                reeval = true;
                                continue;
                            }

                            var pathToCorner = _aStar.FindPath(this, Position, sidePos);
                            if (pathToCorner == null)
                            {
                                CornersBack = null;
                                continue;
                            }

                            StepEnumerator = Steps(pathToCorner.Reverse()).GetEnumerator();
                            End = sidePos;
                        }

                        if (StepEnumerator.MoveNext())
                        {
                            if (StepEnumerator.Current == Move.StayStill)
                            {
                                StepEnumerator = null;
                                Status = AgentStatus.Seeking;
                                reeval = true;
                                continue;
                            }
                            return StepEnumerator.Current;
                        }
                        if (LastStatus == AgentStatus.Pushing)
                            Status = AgentStatus.Readjusting;
                        else
                        {
                            CornersBack.MoveNext();
                            Status = AgentStatus.Pushing;
                        }

                        StepEnumerator = null;
                        reeval = true;

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } while (reeval);

            return Move.StayStill;
        }
    }
}
