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

using System.Collections.Generic;
using System.Drawing;

namespace Agents
{
    public class Cluster
    {
        public static Cluster GetCluster() { return new Cluster(_count++); }
        private static int _count;

        public readonly int Id;
        private Cluster(int id)
        {
            this.Id = id;
            this.Positions = new LinkedList<Point>();
        }

        public readonly LinkedList<Point> Positions;
        public int Size { get { return Positions.Count; } }

        public class SizeComparer : IComparer<Cluster>
        {
            public int Compare(Cluster x, Cluster y)
            {
                if (x.Size < y.Size)
                    return 1;
                if (x.Size > y.Size)
                    return -1;
                return 0;
            }
        }
    }
}
