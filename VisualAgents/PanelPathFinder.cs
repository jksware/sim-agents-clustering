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

using System.Threading;
using Agents;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualAgents
{
    public enum DrawModeSetup
    {
        None = 0,
        Start = 1,
        End = 2,
        Block = 3
    }

    public partial class PanelPathFinder : UserControl
    {
        private int _gridSize = 20;

        private Font _font;

        public PanelPathFinder()
        {
            End = Point.Empty;
            Start = Point.Empty;
            NodeWeight = 1;
            DrawModeSetup = DrawModeSetup.None;
            InitializeComponent();

            _font = new Font("Verdana", 0.29F * _gridSize, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.DoubleBuffered = true;
            ResetMatrix();
        }

        private byte[,] _matrix = new byte[1024, 1024];
        public byte[,] Matrix
        {
            get { return _matrix; }
        }

        private BoardObject[,] _board;
        public BoardObject[,] Board
        {
            get { return _board; }
            set { _board = value; }
        }

        public int GridSize
        {
            get { return _gridSize; }
            set
            {
                _gridSize = value;
                Invalidate();
            }
        }

        public DrawModeSetup DrawModeSetup { get; set; }
        public byte NodeWeight { get; set; }
        public Point Start { get; set; }
        public Point End { get; set; }

        public void ResetMatrix()
        {
            for (int i = 0; i < _matrix.GetUpperBound(0); i++)
                for (int j = 0; j < _matrix.GetUpperBound(1); j++)
                    _matrix[i, j] = 1;

            Start = Point.Empty;
            End = Point.Empty;
        }

        public void DrawDebug(int parentX, int parentY, int x, int y, PathFinderNodeType type, int totalCost, int cost)
        {
            Application.DoEvents();
            Thread.Sleep(10);

            Color c = Color.Empty;
            switch (type)
            {
                case PathFinderNodeType.Close:
                    c = Color.DarkSlateBlue;
                    break;
                case PathFinderNodeType.Current:
                    c = Color.Red;
                    break;
                case PathFinderNodeType.End:
                    c = Color.Red;
                    break;
                case PathFinderNodeType.Open:
                    c = Color.Green;
                    break;
                case PathFinderNodeType.Path:
                    c = Color.Blue;
                    break;
                case PathFinderNodeType.Start:
                    c = Color.Green;
                    break;
            }

            using (var g = Graphics.FromHwnd(this.Handle))
            {
                var rectangle = new Rectangle((x * _gridSize) + 2, (y * _gridSize) + 2, _gridSize - 4, _gridSize - 4);

                if (type == PathFinderNodeType.Open)
                    using (Brush brush = new SolidBrush(Color.FromArgb(255, 240, 240, 240)))
                        g.FillRectangle(brush, rectangle);

                using (Pen pen = new Pen(c))
                    g.DrawRectangle(pen, rectangle);

                if (type == PathFinderNodeType.Open)
                    g.DrawLine(Pens.Brown, (parentX * _gridSize) + _gridSize / 2, (parentY * _gridSize) + _gridSize / 2, (x * _gridSize) + _gridSize / 2, (y * _gridSize) + _gridSize / 2);

                if (type == PathFinderNodeType.Path)
                    using (Brush brush = new SolidBrush(c))
                        g.FillRectangle(brush, rectangle);

                if (totalCost == -1)
                    return;

                rectangle.Inflate(new Size(1, 1));
                rectangle.Height /= 2;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.DrawString(totalCost.ToString(), _font, Brushes.Black, rectangle);
                rectangle.Y += rectangle.Height;
                g.DrawString(cost.ToString(), _font, Brushes.Black, rectangle);
            }
        }

        #region Overrides
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (_matrix != null && _board != null)
            {
                for (int y = e.ClipRectangle.Y / _gridSize * _gridSize, sy = y / _gridSize; y <= e.ClipRectangle.Bottom && sy < _board.GetLength(1); y += _gridSize, sy = y / _gridSize)
                    for (int x = e.ClipRectangle.X / _gridSize * _gridSize, sx = x / _gridSize; x <= e.ClipRectangle.Right && sx < _board.GetLength(0); x += _gridSize, sx = x / _gridSize)
                    {
                        if (_board[sx, sy] != null)
                        {
                            if (_board[sx, sy] is Agent)
                            {
                                g.FillPie(Brushes.LightBlue, x + 1, y + 1, _gridSize - 2, _gridSize - 2, 22, 315);
                                g.DrawPie(Pens.MidnightBlue, x + 1, y + 1, _gridSize - 2, _gridSize - 2, 22, 315);
                            }

                            if (_board[sx, sy] is Gold)
                            {
                                g.FillEllipse(Brushes.Gold, x + 1, y + 1, _gridSize - 2, _gridSize - 2);
                                g.DrawEllipse(Pens.DarkGoldenrod, x + 1, y + 1, _gridSize - 2, _gridSize - 2);
                            }

                            Rectangle rect = new Rectangle(x + 2, y + 5, _gridSize - 2, _gridSize - 2);

                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                            g.DrawString(_board[sx, sy].Name, _font, Brushes.Black, rect);
                            continue;
                        }

                        // Lets render the obstacules
                        Color color;
                        if (_matrix[sx, sy] != 0)
                        {
                            int colorIndex = 240 - ((int)(Math.Log10(_matrix[sx, sy]) * 127));
                            colorIndex = colorIndex < 0 ? 0 : colorIndex > 255 ? 255 : colorIndex;
                            color = Color.FromArgb(255, colorIndex, colorIndex, colorIndex);
                        }
                        else
                            color = Color.Olive;

                        using (SolidBrush brush = new SolidBrush(color))
                            g.FillRectangle(brush, x, y, _gridSize, _gridSize);

                        //Lets render start and end
                        if (sx == Start.X && sy == Start.Y)
                            using (SolidBrush brush = new SolidBrush(Color.Green))
                                g.FillRectangle(brush, x, y, _gridSize, _gridSize);

                        if (sx == End.X && sy == End.Y)
                            using (SolidBrush brush = new SolidBrush(Color.Red))
                                g.FillRectangle(brush, x, y, _gridSize, _gridSize);
                    }
            }

            Color c = Color.LightGray;
            using (Pen pen = new Pen(c))
            {
                for (int y = (e.ClipRectangle.Y / _gridSize) * _gridSize; y <= e.ClipRectangle.Bottom; y += _gridSize)
                    g.DrawLine(pen, e.ClipRectangle.X, y, e.ClipRectangle.Right, y);

                for (int x = (e.ClipRectangle.X / _gridSize) * _gridSize; x <= e.ClipRectangle.Right; x += _gridSize)
                    g.DrawLine(pen, x, e.ClipRectangle.Y, x, e.ClipRectangle.Bottom);
            }

            base.OnPaint(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None || DrawModeSetup == DrawModeSetup.None)
                return;

            int x = e.X / _gridSize;
            int y = e.Y / _gridSize;

            switch (DrawModeSetup)
            {
                case DrawModeSetup.Start:
                    this.Invalidate(new Rectangle(Start.X * _gridSize, Start.Y * _gridSize, _gridSize, _gridSize));
                    Start = new Point(x, y);
                    _matrix[x, y] = 1;
                    break;
                case DrawModeSetup.End:
                    this.Invalidate(new Rectangle(End.X * _gridSize, End.Y * _gridSize, _gridSize, _gridSize));
                    End = new Point(x, y);
                    _matrix[x, y] = 1;
                    break;
                case DrawModeSetup.Block:
                    if (e.Button == (MouseButtons.Left | MouseButtons.Right))
                        _matrix[x, y] = (byte)(_matrix[x, y] - NodeWeight > 1 ? _matrix[x, y] - NodeWeight : 1);
                    else if (e.Button == MouseButtons.Left)
                        _matrix[x, y] = NodeWeight;
                    else if (e.Button == MouseButtons.Right)
                        _matrix[x, y] = (byte)(_matrix[x, y] + NodeWeight < 256 ? _matrix[x, y] + NodeWeight : 255);
                    break;
            }

            this.Invalidate(new Rectangle(x * _gridSize, y * _gridSize, _gridSize, _gridSize));
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.OnMouseMove(e);
            base.OnMouseDown(e);
        }
        #endregion
    }
}
