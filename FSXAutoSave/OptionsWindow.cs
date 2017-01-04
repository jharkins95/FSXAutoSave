//
// FSXAutoSave options dialog
// Author: Jack Harkins
//

using System;
using System.Windows.Forms;

namespace FSXAutoSave
{
    public partial class OptionsWindow : Form
    {

        private FSXClient fsx;

        public OptionsWindow(FSXClient fsx)
        {
            InitializeComponent();
            this.fsx = fsx;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Console.WriteLine("Options window closed");
            this.Hide();
            e.Cancel = true;
        }

        private void checkBoxSaveWhilePaused_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSaveWhilePaused.Checked)
            {
                fsx.enableSaveWhilePaused();
            }
            else
            {
                fsx.disableSaveWhilePaused();
            }
        }

        private void selectorMaxNumSavesToKeep_ValueChanged(object sender, EventArgs e)
        {
            fsx.setMaxNumSavesToKeep((int)selectorMaxNumSavesToKeep.Value);
        }

        private void selectorSaveInterval_ValueChanged(object sender, EventArgs e)
        {
            fsx.setSaveInterval((int)selectorSaveInterval.Value);
        }

        public void loadSettings()
        {
            selectorSaveInterval.Value = Properties.Settings.Default.SaveInterval;
            selectorMaxNumSavesToKeep.Value = Properties.Settings.Default.MaxNumSaves;
            checkBoxSaveWhilePaused.Checked = Properties.Settings.Default.SaveWhilePaused;
        }

        private void buttonSaveSettings_Click(object sender, EventArgs e)
        {
            fsx.saveSettings();
        }
    }
}
