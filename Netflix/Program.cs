using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using InputInterceptorNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Netflix
{
    class Program
    {
        private static Window window;
        public static Element currentHyperlink;
        private static Overlay overlay;
        private static bool overlayInvoked;
        private static List<Element> hyperlinks;
        public static bool animating;
        public static UIA3Automation automation;
        private static ITreeWalker treeWalker;

        static void Main(string[] args)
        {
            // Check if the driver is installed
            if (InputInterceptor.CheckDriverInstalled())
            {
                Console.WriteLine("Input interceptor seems to be installed.");
                // Generate a temporary dll for communication with the driver
                if (InputInterceptor.Initialize())
                {
                    overlay = new Overlay();
                    Hooks.keyboardHookId = Hooks.SetHook(Hooks.keyboardProc);

                    Console.WriteLine("Input interceptor successfully initialized.");
                    // Create simple hooks with outputting all actions to console 
                    MouseHook mouseHook = new MouseHook((ref MouseStroke mouseStroke) =>
                    {
                        Clear();
                    });

                    var originalApp = FlaUI.Core.Application.LaunchStoreApp("4DF9E0F8.Netflix_mcm4njqhnhss8!Netflix.App");
                    using (automation = new UIA3Automation())
                    {
                        Process[] processes = Process.GetProcesses();

                        Process appFrameHost = processes.First(t => t.ProcessName.Contains("ApplicationFrameHost"));

                        var app = FlaUI.Core.Application.Attach(appFrameHost);

                        window = app.GetMainWindow(automation);
                        treeWalker = new UIA3TreeWalkerFactory(automation).GetContentViewWalker();

                        Retry.WhileException(() =>
                        {
                            // var unused =
                            //     window.FindAllChildren()[1].FindAllChildren()[0].FindAllChildren()[0].FindAllChildren()[0]
                            //         .Name;
                            //UpdateLinks();
                            //var unused2 = hyperlinks.Where(h => h.Parent?.Parent?.Properties.AriaRole.ValueOrDefault == "main")
                            //    .FirstOrDefault().Name;
                            Reset();
                            if (window == null)
                            {
                                throw new Exception();
                            }
                        }, TimeSpan.FromSeconds(30), null, true);

                        Thread.Sleep(2000);
                        UpdateLinks();

                        var mon = new Monitor("WWAHost");
                        mon.ProgramClosed += Mon_ProgramClosed;
                    }

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        Thread.Sleep(1000);
                        overlay.AddTrackedWindow(currentHyperlink);
                        overlayInvoked = true;
                    }).Start();

                    System.Windows.Forms.Application.Run(overlay);

                    // Dispose internal filters
                    mouseHook.Dispose();
                    // Cleanup system from temporary dll
                    InputInterceptor.Dispose();
                    Hooks.UnhookWindowsHookEx(Hooks.keyboardHookId);
                }
                else
                {
                    Console.WriteLine("Input interceptor initialization failed.");
                }
            }
           
        }


        private static void Mon_ProgramClosed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }


        public static void Select(Element newLink)
        {
            if (newLink == null)
                return;

            if (currentHyperlink != null)
                overlay.RemoveTrackedWindow();

            currentHyperlink = newLink;
            if (currentHyperlink.AutomationElement.Patterns.ScrollItem.IsSupported)
                currentHyperlink.AutomationElement.Patterns.ScrollItem.Pattern.ScrollIntoView();

            try
            {
                if (currentHyperlink.AutomationElement.Parent.Patterns.Scroll.IsSupported)
                {
                    //currentHyperlink.AutomationElement.Parent.Patterns.Scroll.Pattern.Scroll(ScrollAmount.NoAmount,
                    //    ScrollAmount.SmallDecrement);
                    //currentHyperlink.AutomationElement.Parent.Patterns.Scroll.Pattern.Scroll(ScrollAmount.NoAmount,
                    //    ScrollAmount.SmallIncrement);
                }
                else
                {
                    var elem = currentHyperlink.AutomationElement;
                    while (elem.Parent != null && !elem.Patterns.Scroll.IsSupported)
                    {
                        elem = elem.Parent;
                    }

                    if (elem != null && elem.Patterns.Scroll.IsSupported)
                    {
                        //elem.Patterns.Scroll.Pattern.Scroll(ScrollAmount.NoAmount,
                        //    ScrollAmount.SmallDecrement);
                    }
                }
            }
            catch (Exception e) { }

            if (currentHyperlink.Name != "Keep playing" && overlayInvoked)
            {
                overlay.AddTrackedWindow(currentHyperlink);
            }

            if (currentHyperlink.AutomationElement.IsAvailable && currentHyperlink.AutomationElement.ControlType == ControlType.ListItem)
            {
                if (currentHyperlink.Next.ControlType != ControlType.ListItem)
                {
                    UpdateLinks();
                }
            }
        }

        public static Element MoveDown()
        {
            if (currentHyperlink?.AutomationElement is not { IsAvailable: true })
            {
                Reset();
                return currentHyperlink;
            }

            try
            {
                return hyperlinks.Where(h =>
                    h.ClickablePoint.Y > currentHyperlink.ClickablePoint.Y)
                    .FirstOrDefault();
            }
            catch
            {
                UpdateLinks();
                return MoveDown();
            }
        }

        public static Element MoveUp()
        {
            if (currentHyperlink?.AutomationElement is not { IsAvailable: true })
            {
                Reset();
                return currentHyperlink;
            }

            try
            {
                var lastInRowAbove = hyperlinks.LastOrDefault(h =>
                    h.ClickablePoint.Y < currentHyperlink.ClickablePoint.Y);
                return hyperlinks.FirstOrDefault(h =>
                    h.ClickablePoint.Y == lastInRowAbove?.ClickablePoint.Y);
            }
            catch
            {
                UpdateLinks();
                return MoveUp();
            }
        }

        public static Element MoveLeft(Element toCheck)
        {
            if (currentHyperlink == null || !(toCheck?.AutomationElement.IsAvailable ?? false))
            {
                Reset();
                return currentHyperlink;
            }

            var left = hyperlinks.Where(h =>
                h.ClickablePoint.Y == toCheck.ClickablePoint.Y &&
                h.ClickablePoint.X < toCheck.ClickablePoint.X && h.Name != "More")
                .LastOrDefault();

            if (left?.BoundingRectangle.Y != toCheck.BoundingRectangle.Y)
            {
                left = hyperlinks.FirstOrDefault(h =>
                    h.ClickablePoint.Y == toCheck.ClickablePoint.Y &&
                    h.Name == "Previous");
            }

            if (left?.Name == "Previous")
            {
                overlay.RemoveTrackedWindow();
                left.AutomationElement.Patterns.Invoke.Pattern.Invoke();
                var toCheckLocation = toCheck.ClickablePoint;
                animating = true;
                Thread.Sleep(1000);
                UpdateLinks();
                toCheck = hyperlinks.FirstOrDefault(h =>
                    h.Name == "More" && h.ClickablePoint.Y == toCheckLocation.Y);
                animating = false;
                return MoveLeft(toCheck);
            }

            return left;
        }

        public static Element MoveRight(Element toCheck)
        {
            if (currentHyperlink == null || !(toCheck?.AutomationElement.IsAvailable ?? false))
            {
                Reset();
                return currentHyperlink;
            }

            var right = hyperlinks.Where(h =>
                h.ClickablePoint.Y == toCheck.ClickablePoint.Y &&
                h.ClickablePoint.X > toCheck.ClickablePoint.X &&
                h.Name != "Previous")
                .FirstOrDefault();

            if (right?.Name == "More")
            {
                overlay.RemoveTrackedWindow();
                right.AutomationElement.Patterns.Invoke.Pattern.Invoke();
                var toCheckName = toCheck.Name;
                var toCheckLocation = toCheck.ClickablePoint;
                animating = true;
                Thread.Sleep(1000);
                UpdateLinks();
                toCheck = hyperlinks.FirstOrDefault(h =>
                    h.Name == toCheckName && h.ClickablePoint.Y == toCheckLocation.Y);
                animating = false;
                return MoveRight(toCheck);
            }

            return right;
        }

        public static void UpdateLinks()
        {
            if (window == null)
                return;
            animating = true;
            hyperlinks = window
                .FindAllDescendants(cf =>
                    cf
                    .ByControlType(ControlType.Group)
                    .Or(cf.ByControlType(ControlType.Hyperlink))
                    .Or(cf.ByControlType(ControlType.Button)
                    .Or(cf.ByControlType(ControlType.ListItem))
                    .Or(cf.ByControlType(ControlType.ComboBox)))).Where(c =>
                    (c.ControlType != ControlType.Group || c.FindAllChildren().Length != 0) &&
                    c.Patterns.Invoke.IsSupported && !c.Patterns.Scroll.IsSupported && c.AutomationId != "Minimize" &&
                    c.AutomationId != "Maximize" && c.AutomationId != "Restore" && c.AutomationId != "Close" &&
                    c.Properties.AriaRole.ValueOrDefault != "main" &&
                    c.Properties.AriaRole.ValueOrDefault != "navigation" &&
                    (c.Parent == null || (c.Parent.Properties.AriaRole.ValueOrDefault != "navigation" && c.Parent.Properties.AriaRole.ValueOrDefault != "heading")) &&
                    (c.Parent == null || c.Properties.AriaRole.ValueOrDefault == "alert" || c.Parent.ControlType != ControlType.List || c.Name != "" || c.ControlType != ControlType.Group))
                    .Select(h => new Element(h, treeWalker))
                    .ToList();

            var comboBox = hyperlinks.FirstOrDefault(h => h.ControlType == ControlType.ComboBox && h.AutomationElement.AsComboBox().ExpandCollapseState == ExpandCollapseState.Expanded);
            if (comboBox != null)
            {
                var list = window.FindAllDescendants(cf => cf.ByControlType(ControlType.List)).FirstOrDefault(l => l.FindAllChildren().Select(c => c.Name).Contains(comboBox.Name));
                if (list != null)
                {
                    hyperlinks = list.FindAllChildren().Select(h => new Element(h, treeWalker)).ToList();
                }
            }

            hyperlinks = hyperlinks.Where(h => h.AutomationElement.Properties.AriaRole.ValueOrDefault != "alert" || h.AutomationElement.FindFirstChild() == null || !h.AutomationElement.FindFirstChild().Name.StartsWith("on")).ToList();

            var alertIndex = hyperlinks.FindIndex(h => h.AutomationElement.Properties.AriaRole.ValueOrDefault == "alert");
            if (alertIndex != -1)
            {
                hyperlinks = hyperlinks.GetRange(alertIndex, hyperlinks.Count - alertIndex);
            }

            hyperlinks = hyperlinks.Where(h => h.AutomationElement.Properties.AriaRole.ValueOrDefault != "alert").ToList();

            if (currentHyperlink != null)
            {
                currentHyperlink = hyperlinks.FirstOrDefault(h =>
                    h.Name == currentHyperlink.Name);
            }
            animating = false;
        }

        public static void Reset()
        {
            Clear();

            UpdateLinks();

            currentHyperlink = //hyperlinks
                                   //.FirstOrDefault(h =>
                                       //h.Parent?.Parent?.Properties.AriaRole.ValueOrDefault == "main") ??
                               hyperlinks.FirstOrDefault();

            if (currentHyperlink == null)
            {
                Thread.Sleep(500);
                Reset();
                return;
            }

            currentHyperlink.AutomationElement.Patterns.ScrollItem.Pattern.ScrollIntoView();
            if (currentHyperlink.AutomationElement.Parent.Patterns.Scroll.IsSupported)
            {
                //currentHyperlink.AutomationElement.Parent.Patterns.Scroll.Pattern.Scroll(ScrollAmount.NoAmount,
                //    ScrollAmount.SmallDecrement);
            }

            if (currentHyperlink?.Name != "Keep playing" && overlayInvoked)
            {
                overlay.AddTrackedWindow(currentHyperlink);
            }
        }

        public static void Clear()
        {
            if (currentHyperlink != null && overlayInvoked && overlay.OverlayCount() != 0)
            {
                overlay.RemoveTrackedWindow();
            }

            currentHyperlink = null;
        }

        public static void Click()
        {
            UpdateLinks();
            if (currentHyperlink != null && currentHyperlink.AutomationElement.Patterns.Invoke.IsSupported)
            {
                currentHyperlink?.AutomationElement.Patterns.Invoke.Pattern.Invoke();
            }

            Console.WriteLine("Click");

            Clear();


            new Thread(() =>
            {
                Thread.Sleep(1000);

                Reset();
            }).Start();

        }
    }
}
