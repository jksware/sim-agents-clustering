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
    public class World
    {
        private readonly HashSet<Team> _teams;
        private readonly List<Team> _teamList;
        private readonly HashSet<Agent> _agents;
        private readonly List<Agent> _agentList;
        private readonly HashSet<Gold> _gold;

        public int LoopCount { get; private set; }
        public int MaxLoops { get; set; }

        private readonly BoardObject[,] _board;
        public BoardObject[,] Board { get { return _board; } }

        public BoardObject this[int x, int y]
        {
            get { return _board[x, y]; }
        }

        public BoardObject this[Point p]
        {
            get { return _board[p.X, p.Y]; }
        }

        public int CostOf(int x, int y)
        {
            return 1;
        }

        public int CostOf(Point p)
        {
            return 1;
        }

        public int Length { get; private set; }

        public /* event */ PathFinderDebugHandler PathFinderDebug;

        /// <summary>
        /// Creates a world with a squared board of size length.
        /// </summary>
        /// <param name="length"></param>
        public World(int length)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException("Length must be greater than 0");

            MaxLoops = 1000;
            Length = length;
            _board = new BoardObject[length, length];
            _teams = new HashSet<Team>();
            _teamList = new List<Team>();
            _agents = new HashSet<Agent>();
            _agentList = new List<Agent>();
            _gold = new HashSet<Gold>();
        }


        /// <summary>
        /// Adds a team (in order to add agents and gold later on)
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public bool AddTeam(Team team)
        {
            _teamList.Add(team);
            return _teams.Add(team);
        }

        /// <summary>
        /// Adds an agent to the board
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public bool AddAgent(Agent agent)
        {
            if (agent == null)
                throw new ArgumentNullException("Agent cannot be null");

            if (!_teams.Contains(agent.Team))
                // should add team first
                return false;

            agent.Team.AddAgent(agent);
            _agentList.Add(agent);
            _board[agent.Position.X, agent.Position.Y] = agent;
            return _agents.Add(agent);
        }

        /// <summary>
        /// Adds gold to the board
        /// </summary>
        /// <param name="gold"></param>
        /// <returns></returns>
        public bool AddGold(Gold gold)
        {
            if (gold == null)
                throw new ArgumentNullException("Object cannot be null");

            if (!_teams.Contains(gold.Team))
                // should add team first
                return false;

            _board[gold.Position.X, gold.Position.Y] = gold;

            return _gold.Add(gold);
        }

        /// <summary>
        /// Creates a new map with the team, agents a golds count given.
        /// </summary>
        /// <param name="teams"></param>
        /// <param name="agentsPerTeam"></param>
        /// <param name="goldsPerTeam"></param>
        public void Randomize(int teams, int agentsPerTeam, int goldsPerTeam)
        {
            _teamList.Clear();
            _teams.Clear();
            _agentList.Clear();
            _agents.Clear();
            _gold.Clear();
            for (int i = 0; i < Length; i++)
                for (int j = 0; j < Length; j++)
                    _board[i, j] = null;

            if (teams * (agentsPerTeam + goldsPerTeam) >= Length * Length)
                throw new ArgumentException("The configuration would never be met for the board size used (there is no enough room).");

            var rnd = new Random();
            var taken = new bool[Length, Length];

            for (int i = 0; i < teams; i++)
            {
                var team = new Team() { Name = string.Format("T{0}", i) };
                AddTeam(team);

                for (int j = 0; j < goldsPerTeam; j++)
                {
                    Point pos;
                    do pos = new Point(rnd.Next(1, Length - 1), rnd.Next(1, Length - 1));
                    while (taken[pos.X, pos.Y]);

                    taken[pos.X, pos.Y] = true;
                    AddGold(new Gold(this, team, pos) { Name = string.Format("{0}T{1}", j, i) });
                }

                var teamClusterList = GetClusters(team).ToList();
                int max = -1, maxValue = 0;
                for (int j = 0; j < teamClusterList.Count; j++)
                    if (maxValue < teamClusterList[i].Positions.Count)
                    {
                        maxValue = teamClusterList[i].Positions.Count;
                        max = i;
                    }

                var teamCluster = teamClusterList[max];
                teamClusterList.RemoveAt(max);

                int clusterPerAgent = teamClusterList.Count / agentsPerTeam;

                for (int j = 0; j < agentsPerTeam; j++)
                {
                    Point pos;
                    do pos = new Point(rnd.Next(Length), rnd.Next(Length));
                    while (taken[pos.X, pos.Y]);

                    taken[pos.X, pos.Y] = true;
                    var agent = new ClusterAgent(this, team, pos)
                                    {
                                        Name = string.Format("{0}T{1}", j, i),
                                        TeamCluster = teamCluster
                                    };
                    AddAgent(agent);

                    for (int k = j * clusterPerAgent; k < (j + 1) * clusterPerAgent; k++)
                        if (k != max)
                            agent.Clusters.Add(teamClusterList[k]);
                }
            }
        }

        public bool Exists(Point position)
        {
            int px = position.X;
            int py = position.Y;
            return px >= 0 && py >= 0 && px < Length && py < Length;
        }

        /// <summary>
        /// Validates a position (says whether the position is empty or not)
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        public bool CanMove(Point newPosition)
        {
            int px = newPosition.X;
            int py = newPosition.Y;

            return px >= 0 && py >= 0 && px < Length && py < Length && _board[px, py] == null;
        }

        /// <summary>
        /// Validates a position and a move to a new position (says whether the position is empty or not)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="move"></param>
        /// <returns></returns>
        public bool CanMove(Point position, Move move)
        {
            int px = position.X + move.Dx;
            int py = position.Y + move.Dy;

            return px >= 0 && py >= 0 && px < Length && py < Length && _board[px, py] == null;
        }

        /// <summary>
        /// Gets all the clusters in the scene
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public IEnumerable<Cluster> GetClusters(Team team)
        {
            var clusters = new HashSet<Cluster>();

            foreach (var gold in _gold)
                gold.Cluster = null;

            for (int i = 0; i < Length; i++)
                for (int j = 0; j < Length; j++)
                {
                    var cluster = ClusterAt(team, new Point(i, j));
                    if (cluster != null)
                        clusters.Add(cluster);
                }

            return clusters;
        }

        /// <summary>
        /// Returns the cluster at the given position for a team
        /// </summary>
        /// <param name="team"></param>
        /// <param name="clusterLocation"></param>
        /// <returns></returns>
        public Cluster ClusterAt(Team team, Point clusterLocation)
        {
            var location = clusterLocation;

            if (location.X < 0 || location.Y < 0 || location.X >= Length || location.Y >= Length)
                return null;

            var gold = _board[location.X, location.Y] as Gold;
            if (gold == null || !gold.Team.Equals(team))
                return null;

            var queue = new Queue<Point>();
            queue.Enqueue(location);

            var visited = new bool[Length, Length];

            if (gold.Cluster != null)
                return gold.Cluster;

            var cluster = Cluster.GetCluster();

            while (queue.Count > 0)
            {
#if DEBUG
                if (PathFinderDebug != null)
                    PathFinderDebug(location.X, location.Y, location.X, location.Y, PathFinderNodeType.Close, -1, -1);
#endif

                location = queue.Dequeue();
                ((Gold)_board[location.X, location.Y]).Cluster = cluster;

                visited[location.X, location.Y] = true;
                cluster.Positions.AddLast(location);

                foreach (var move in Utils.Movements)
                {
                    var newLocation = location + move;

#if DEBUG
                    if (PathFinderDebug != null)
                        PathFinderDebug(location.X, location.Y, newLocation.X, newLocation.Y, PathFinderNodeType.Open, -1, -1);
#endif

                    if (newLocation.X < 0 || newLocation.Y < 0 || newLocation.X >= Length || newLocation.Y >= Length)
                        continue;

                    var goldAtPosition = _board[newLocation.X, newLocation.Y] as Gold;
                    if (visited[newLocation.X, newLocation.Y] || goldAtPosition == null || !goldAtPosition.Team.Equals(team))
                        continue;

                    queue.Enqueue(newLocation);
                }
            }

            return cluster;
        }

        /// <summary>
        /// Performs the game logic.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tuple<Agent, Move>> Loop()
        {
            int moveCount = 0;

            foreach (var agent in _agentList.Cast<ClusterAgent>())
            {
                if (agent.TeamCluster.Positions.Count == 0)
                    agent.Status = ClusterAgent.AgentStatus.Idle;
                else
                {
                    // the last one is the right one
                    agent.Base = agent.TeamCluster.Positions.Last();
                }
            }

            for (LoopCount = 0; LoopCount < MaxLoops || MaxLoops == -1; LoopCount++)
            {
                foreach (var team in _teamList)
                {
                    var agentShuffle = new Agent[team.AgentList.Count];
                    team.AgentList.CopyTo(agentShuffle);
                    Utils.FisherYatesPermutation(agentShuffle);

                    foreach (var agent in agentShuffle)
                    {
                        var move = agent.ComputeNextMove();
                        var newPosition = agent.Position + move;
                        Gold newHat = null;

                        if (Exists(newPosition))
                            newHat = _board[newPosition.X, newPosition.Y] as Gold;

                        if (move == Move.StayStill || !CanMove(newPosition) && newHat == null)
                        {
                            yield return new Tuple<Agent, Move>(agent, Move.StayStill);
                            continue;
                        }

                        moveCount++;

                        _board[agent.Position.X, agent.Position.Y] = null;
                        _board[newPosition.X, newPosition.Y] = agent;
                        agent.Position = newPosition;

                        if (newHat != null)
                        {
                            var newHatPosition = newPosition + move;
                            _board[newHatPosition.X, newHatPosition.Y] = newHat;
                            newHat.Position = newHatPosition;
                        }

                        yield return new Tuple<Agent, Move>(agent, move);
                    }
                }
                // todo mejorar simplificacion
                //if (moveCount == 0)
                //break;

                moveCount = 0;
            }
        }
    }
}
