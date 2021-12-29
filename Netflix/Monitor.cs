using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Netflix
{
    class Monitor
    {
        public event EventHandler ProgramStarted;
        public event EventHandler ProgramClosed;

        public Monitor(string process)
        {
            string pol = "2";
            if (!process.EndsWith(".exe")) process += ".exe";

            var queryString =
                "SELECT *" +
                "  FROM __InstanceOperationEvent " +
                "WITHIN  " + pol +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + process + "'";

            var s = @"\\.\root\CIMV2";
            ManagementEventWatcher watcher = new ManagementEventWatcher(s, queryString);
            watcher.EventArrived += new EventArrivedEventHandler(OnEventArrived);
            watcher.Start();
        }
        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.ClassPath.ClassName.Contains("InstanceDeletionEvent"))
            {
                EventHandler handler = ProgramClosed;
                handler?.Invoke(this, e);
            }
            else if (e.NewEvent.ClassPath.ClassName.Contains("InstanceCreationEvent"))
            {
                EventHandler handler = ProgramStarted;
                handler?.Invoke(this, e);
            }

        }
    }
}
