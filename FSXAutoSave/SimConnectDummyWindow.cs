//
// Dummy window for handling events from SimConnect
// Author: Jack Harkins
//

using System.Windows.Forms;

namespace FSXAutoSave
{
    public partial class SimConnectDummyWindow : Form
    {

        private FSXClient client;
        public const int WM_USER_SIMCONNECT = 0x0402; // window handle ID

        public SimConnectDummyWindow()
        {
            InitializeComponent();
        }

        public void registerSimConnect(FSXClient client)
        {
            this.client = client;
        }

        // Top-level message handling from FSX
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                if (client != null)
                {
                    client.receiveMessage();  
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }
    }
}
