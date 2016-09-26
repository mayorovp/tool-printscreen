using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreen
{
    public class SelectionFrame : Form
    {
        private readonly Pen pen = new Pen(Color.Blue, 2.5f);

        public SelectionFrame()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopLevel = true;
            this.Cursor = Cursors.Cross;
        }

        private Bitmap captured;

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                startPoint = null;

                if (FormBorderStyle == FormBorderStyle.None)
                {
                    Bounds = Rectangle.FromLTRB(
                        Screen.AllScreens.Min(s => s.Bounds.Left),
                        Screen.AllScreens.Min(s => s.Bounds.Top),
                        Screen.AllScreens.Max(s => s.Bounds.Right),
                        Screen.AllScreens.Max(s => s.Bounds.Bottom));
                }

                captured = new Bitmap(ClientSize.Width, ClientSize.Height);
                using (var g = Graphics.FromImage(captured))
                    g.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, ClientSize);

				TopMost = true;
            }
            else
            {
                captured.Dispose();
                captured = null;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape) Hide();
        }

        private Point? startPoint;
        private Point lastPosition;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (startPoint == null)
                {
                    startPoint = e.Location;
                    lastPosition = e.Location;
                }
                else
                {
                    TakeScreenshot(GetRect(startPoint.Value, e.Location));
                    Hide();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                startPoint = null;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (startPoint == null) return;
            using (var g = CreateGraphics())
            {
                foreach (var rect in GetDifferecne(startPoint.Value, e.Location, lastPosition))
                {
                    rect.Inflate((int)Math.Ceiling(pen.Width), (int)Math.Ceiling(pen.Width));
                    g.DrawImage(captured, rect, rect, GraphicsUnit.Pixel);
                }
                g.DrawRectangle(pen, GetRect(startPoint.Value, e.Location));
                lastPosition = e.Location;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (startPoint == null) return;
            e.Graphics.DrawRectangle(pen, GetRect(startPoint.Value, lastPosition));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.DrawImage(captured, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }

        private static Rectangle GetRect(Point p1, Point p2)
        {
            return GetRect(p1.X, p1.Y, p2.X, p2.Y);
        }

        private static Rectangle GetRect(int x1, int y1, int x2, int y2)
        {
            return Rectangle.FromLTRB(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2));
        }

        private static Rectangle GetRect(Point a, Point b, Point c)
        {
            return Rectangle.FromLTRB(
                Math.Min(Math.Min(a.X, b.X), c.X),
                Math.Min(Math.Min(a.Y, b.Y), c.Y),
                Math.Max(Math.Max(a.X, b.X), c.X),
                Math.Max(Math.Max(a.Y, b.Y), c.Y));
        }

        private static IEnumerable<Rectangle> GetDifferecne(Point s, Point a, Point b)
        {
            var bounds = GetRect(s, a, b);
            yield return Rectangle.FromLTRB(bounds.Left, Math.Min(a.Y, b.Y), bounds.Right, Math.Max(a.Y, b.Y));
            yield return Rectangle.FromLTRB(Math.Min(a.X, b.X), bounds.Top, Math.Max(a.X, b.X), bounds.Bottom);
        }

        private void TakeScreenshot(Rectangle rectangle)
        {
            using (var bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                    g.DrawImage(captured, 0, 0, rectangle, GraphicsUnit.Pixel);
                Clipboard.SetImage(bitmap);
            }
        }
    }
}