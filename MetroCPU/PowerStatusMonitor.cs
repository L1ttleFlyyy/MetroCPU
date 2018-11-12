using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Forms;

namespace MetroCPU
{
    class PowerStatusMonitor
    {
        public bool IsEnabled;
        private Action OnlineAction, OfflineAction;
        public PowerStatusMonitor(Action onlineAction,Action offlineAction)
        {
            OnlineAction = onlineAction;
            OfflineAction = offlineAction;
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler((s,e)=>
            {
                if(IsEnabled)
                {
                    switch(e.Mode)
                    {
                        case PowerModes.Resume:
                        case PowerModes.StatusChange:
                            ChangePowerMode();
                            break;
                        default:
                            break;
                            
                    }
                }
            });
        }

        private void ChangePowerMode()
        {
            if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline)

                OfflineAction();
            else
                OnlineAction();
        }
        public PowerLineStatus GetPowerLineStatus()
        {
            return SystemInformation.PowerStatus.PowerLineStatus;
        }
    }
}
