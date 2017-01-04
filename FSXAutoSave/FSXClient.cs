//
// FSX SimConnect client and autosave functionality
// Author: Jack Harkins
//

using System;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;

namespace FSXAutoSave
{
    enum EVENTS
    {
        SIM_START,
        SIM_STOP,
        SIM_PAUSE,
        EVENT_MENU,
        EVENT_MENU_ENABLE_DISABLE,
        EVENT_MENU_OPTIONS,
    }

    public class FSXClient
    {
        private SimConnect fsx; // The SimConnect client

        private int saveInterval; // seconds
        private const string FILENAME_BASE = "FSXAutoSave_";
        private int maxNumSavesToKeep;

        private bool simRunning = false;
        private bool simPaused = false;
        private bool saveEnabled = false;
        private bool canSaveWhilePaused;
        private System.Timers.Timer saveTimer;

        private SimConnectDummyWindow dummyWindow;
        private OptionsWindow optionsWindow;

        public FSXClient(SimConnectDummyWindow dummyWindow) {
            this.dummyWindow = dummyWindow;
            setupSimConnect();
            optionsWindow = new FSXAutoSave.OptionsWindow(this);
            optionsWindow.Visible = false;
            saveTimer = new System.Timers.Timer();
            saveTimer.Elapsed += new ElapsedEventHandler(saveGame);

            loadSettings();

            fsx.MenuAddItem("FSXAutoSave", EVENTS.EVENT_MENU, 0);
            fsx.MenuAddSubItem(EVENTS.EVENT_MENU, "Enable/Disable", EVENTS.EVENT_MENU_ENABLE_DISABLE, 0);
            fsx.MenuAddSubItem(EVENTS.EVENT_MENU, "Options", EVENTS.EVENT_MENU_OPTIONS, 0);
        }

        public void resetSaveTimer(int interval)
        {
            saveTimer.Stop();
            saveTimer.Interval = interval;
            saveTimer.Start();
        }

        public void loadSettings()
        {
            saveInterval = Properties.Settings.Default.SaveInterval;
            maxNumSavesToKeep = Properties.Settings.Default.MaxNumSaves;
            canSaveWhilePaused = Properties.Settings.Default.SaveWhilePaused;

            resetSaveTimer(1000 * 60 * saveInterval);
            optionsWindow.loadSettings();
            Console.WriteLine("Settings loaded.");
            printSettings();
        }

        public void printSettings()
        {
            Console.WriteLine(string.Format("saveInterval: {0}, maxNumSaves: {1}, saveWhilePaused: {2}",
                Properties.Settings.Default.SaveInterval,
                Properties.Settings.Default.MaxNumSaves,
                Properties.Settings.Default.SaveWhilePaused));
        }

        public void saveSettings()
        {
            Properties.Settings.Default.SaveInterval = saveInterval;
            Properties.Settings.Default.MaxNumSaves = maxNumSavesToKeep;
            Properties.Settings.Default.SaveWhilePaused = canSaveWhilePaused;
            Properties.Settings.Default.Save();
            Console.WriteLine("Settings saved.");
            printSettings();
        }

        public void setupSimConnect()
        {
            try
            {
                fsx = new SimConnect("FSXAutoSave", dummyWindow.Handle, SimConnectDummyWindow.WM_USER_SIMCONNECT, null, 0);
            }
            catch (COMException e)
            {
                MessageBox.Show("Please start FSX before launching this application.");
                Environment.Exit(1);
            }

            // listen to quit msg
            fsx.OnRecvQuit += new SimConnect.RecvQuitEventHandler(fsx_OnRecvQuit);

            // listen to events
            fsx.OnRecvEvent += new SimConnect.RecvEventEventHandler(fsx_OnRecvEvent);

            // Subscribe to system events
            fsx.SubscribeToSystemEvent(EVENTS.SIM_START, "SimStart");
            fsx.SubscribeToSystemEvent(EVENTS.SIM_STOP, "SimStop");
            fsx.SubscribeToSystemEvent(EVENTS.SIM_PAUSE, "Pause");

            // Turn events on
            fsx.SetSystemEventState(EVENTS.SIM_START, SIMCONNECT_STATE.ON);
            fsx.SetSystemEventState(EVENTS.SIM_STOP, SIMCONNECT_STATE.ON);
            fsx.SetSystemEventState(EVENTS.SIM_PAUSE, SIMCONNECT_STATE.ON);
            Console.WriteLine("SimConnect initialized");
        }

        public void closeConnection()
        {
            if (fsx != null)
            {
                fsx.Dispose();
                fsx = null;
            }
        }

        // Simulator event handler
        public void fsx_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            switch (recEvent.uEventID)
            {
                case (uint)EVENTS.SIM_START:
                    Console.WriteLine("Sim started");
                    simRunning = true;
                    break;
                case (uint)EVENTS.SIM_STOP:
                    Console.WriteLine("Sim stopped");
                    simRunning = false;
                    break;
                case (uint)EVENTS.SIM_PAUSE:
                    if (recEvent.dwData == 0) // unpause
                    {
                        Console.WriteLine("Sim unpaused");
                        simPaused = false;
                    }
                    else if (recEvent.dwData == 1) // pause
                    {
                        Console.WriteLine("Sim paused");
                        simPaused = true;
                    }
                    break;
                case (uint)EVENTS.EVENT_MENU_ENABLE_DISABLE:
                    if (saveEnabled)
                    {
                        saveEnabled = false;
                        Console.WriteLine("Autosave disabled.");
                    }
                    else
                    {
                        saveEnabled = true;
                        Console.WriteLine("Autosave enabled.");
                    }
                    break;
                case (uint)EVENTS.EVENT_MENU_OPTIONS:
                    Console.WriteLine("Options menu pressed");
                    showOptionsWindow();
                    break;
            }
        }

        // Close FSX
        public void fsx_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            closeConnection();
            Application.Exit();
        }

        public void saveGame(object sender, EventArgs e)
        {
            if (saveEnabled && simRunning)
            {
                if (canSaveWhilePaused || (!canSaveWhilePaused && !simPaused))
                {
                    string currentTime = DateTime.Now.ToString();
                    // Filter slashes, colons, and spaces
                    currentTime = currentTime.Replace('/', '_').Replace(':', '_').Replace(' ', '_');

                    #if !DEBUG
                        fsx.FlightSave(FILENAME_BASE + currentTime, null, "FSXAutoSave autosaved flight", 0);
                    #endif
                    Console.WriteLine("Game saved: " + currentTime);
                }
            }
        }


        // events raised from options window or 'dummy' FSX event handler window
        public void receiveMessage()
        {
            if (fsx != null)
            {
                fsx.ReceiveMessage();
            }
        }

        public void showOptionsWindow()
        {
            optionsWindow.Visible = true;
        }

        public void hideOptionsWindow()
        {
            optionsWindow.Visible = false;
        }

        public void enableSaveWhilePaused()
        {
            canSaveWhilePaused = true;
            Console.WriteLine("Saving while paused ENABLED.");
        }

        public void disableSaveWhilePaused()
        {
            canSaveWhilePaused = false;
            Console.WriteLine("Saving while paused DISABLED.");
        }

        public void setSaveInterval(int minutes)
        {
            saveInterval = minutes; // seconds for now...
            resetSaveTimer(1000 * 60 * saveInterval);
            Console.WriteLine("Save interval changed to " + minutes + " seconds.");
        }

        public void setMaxNumSavesToKeep(int numSavesToKeep)
        {
            maxNumSavesToKeep = numSavesToKeep;
            Console.WriteLine("Max. number of saves changed to " + maxNumSavesToKeep);
        }
        
    }
}
