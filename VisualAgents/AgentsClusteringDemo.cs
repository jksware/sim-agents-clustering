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

using Agents;
using System;
using System.Threading;
using System.Windows.Forms;

namespace VisualAgents
{
    public partial class AgentsClusteringForm : Form
    {
        protected World World = new World(20);

        public AgentsClusteringForm()
        {
            InitializeComponent();

            panelPathFinder1.Board = this.World.Board;
            this.World.PathFinderDebug += panelPathFinder1.DrawDebug;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void randomizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.listBox1.Items.Clear();
            this.World.Randomize(2, 1, 10);
            this.panelPathFinder1.Refresh();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                foreach (var tuple in this.World.Loop())
                {
                    var agent = tuple.Item1;
                    var move = tuple.Item2;
                    var status = string.Format("Agent {0} [x={1},y={2}] moved {3} in status {4}.",
                                               agent.Name, agent.Position.X, agent.Position.Y, move.Dxy,
                                               ((ClusterAgent) agent).Status);
                    toolStripStatusLabel1.Text = status;
                    listBox1.Items.Add(status);
                    panelPathFinder1.Invalidate();
                    Application.DoEvents();
                    Thread.Sleep(50);
                }
            }
            catch (InvalidOperationException ioe)
            {
                MessageBox.Show(string.Format("The loop has stopped. Reason: {0}", ioe.Message), "Error on loop",
                                MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("VisualAgents (for clustering)\n\n" +
                            "Copyright (c) 2014 Juan Carlos Pujol Mainegra <j.pujol@lab.matcom.uh.cu>\n\n" +
                            "Partially based on a A*-rendering control by (C) 2006 Franco, Gustavo", "About", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }
    }
}
