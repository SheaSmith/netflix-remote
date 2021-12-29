using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Netflix
{
    public class Overlay : Form // standard Windows Form
    {
        private readonly HashSet<Element> _windows = new HashSet<Element>();

        public Overlay()
        {
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            TransparencyKey = BackColor;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_LAYERED = 0x80000;
                const int WS_EX_NOACTIVATE = 0x8000000;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            foreach (var window in _windows.ToArray())
            {
                Rectangle rect;
                try
                {
                    rect = window.AutomationElement.Properties.BoundingRectangle.Value;
                }
                catch
                {
                    // error, window's gone
                    _windows.Clear();
                    continue;
                }

                // draw a yellow rectangle around window
                using (var pen = new Pen(Color.WhiteSmoke, 5))
                {
                    e.Graphics.DrawRectangle(pen, (float)rect.X, (float)rect.Y, (float)rect.Width,
                        (float)rect.Height);
                }
            }
        }

        // ensure we call Invalidate on UI thread
        private void InvokeInvalidate() => BeginInvoke((Action)(() => { Invalidate(); }));

        public void RemoveTrackedWindow()
        {
            _windows.Clear();
            InvokeInvalidate();
        }

        public int OverlayCount()
        {
            return _windows.Count;
        }

        public void AddTrackedWindow(Element element)
        {
            _windows.Clear();
            _windows.Add(element);
            InvokeInvalidate();
        }
    }
}
