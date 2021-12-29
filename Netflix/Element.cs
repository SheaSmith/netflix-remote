using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netflix
{
    public class Element
    {
        public AutomationElement AutomationElement { get; set; }

        public Rectangle BoundingRectangle { get; set; }

        public Point ClickablePoint { get; set; }

        public string Name { get; set; }

        public ControlType ControlType { get; set; }

        public AutomationElement Next { get; set; }

        public Element(AutomationElement element, ITreeWalker treeWalker)
        {
            AutomationElement = element;
            BoundingRectangle = element.BoundingRectangle;
            ClickablePoint = element.Properties.ClickablePoint;
            Name = element.Name;
            ControlType = element.ControlType;
            if (element.ControlType == ControlType.ListItem)
                Next = treeWalker.GetNextSibling(element);
        }
    }
}
