using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VAICOM.KneeboardReceiver
{
    public partial class MainForm : Form, IKneeboardDisplay
    {
        private AdvancedKneeboardManager _manager;

        public MainForm()
        {
            InitializeComponent();

            _manager = Program.KneeboardManager;

            // Setup event handlers
            _manager.GroupsUpdated += Manager_GroupsUpdated;
            _manager.PageChanged += Manager_PageChanged;
            _manager.NightModeToggled += Manager_NightModeToggled;
            _manager.LogMessageReceived += Manager_LogMessageReceived;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadGroups();
            UpdateNightModeButton();
        }

        private void groupListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (groupListBox.SelectedIndex >= 0)
            {
                var groups = _manager.GetAvailableGroups();
                if (groupListBox.SelectedIndex < groups.Count)
                {
                    _manager.LoadGroup(groups[groupListBox.SelectedIndex].Name);
                    DisplayCurrentPage();
                }
            }
        }


        public void ShowMainMenu()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowMainMenu));
            }
            else
            {
                LoadGroups(); // Il tuo metodo esistente che carica la lista
            }
        }
        public void LoadGroups()
        {
            groupListBox.Items.Clear();
            var groups = _manager.GetAvailableGroups();

            foreach (var group in groups)
            {
                string nightIndicator = group.HasNightVersion ? " 🌙" : "";
                groupListBox.Items.Add($"{group.DisplayName}{nightIndicator}");
            }

            if (groupListBox.Items.Count > 0)
            {
                groupListBox.SelectedIndex = 0;
            }
        }

        public void ShowCurrentPage()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowCurrentPage));
            }
            else
            {
                DisplayCurrentPage();
            }
        }
        public void DisplayCurrentPage()
        {
            var currentPage = _manager.GetCurrentPage();
            if (currentPage != null && File.Exists(currentPage.FilePath))
            {
                try
                {
                    // Usa una copia per evitare locking del file
                    using (var tempImage = Image.FromFile(currentPage.FilePath))
                    {
                        pictureBox.Image = new Bitmap(tempImage);
                    }

                    statusLabel.Text = $"Page {_manager.CurrentPageIndex + 1} of {_manager.TotalPages} | {Path.GetFileName(currentPage.FilePath)}";
                }
                catch (Exception ex)
                {
                    pictureBox.Image = null;
                    statusLabel.Text = $"Error loading image: {ex.Message}";
                }
            }
            else
            {
                pictureBox.Image = null;
                statusLabel.Text = "No page available";
            }
        }

        public void LogMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                AddLogMessage(message);
            }
        }
        public void AddLogMessage(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action<string>(AddLogMessage), message);
            }
            else
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                logTextBox.ScrollToCaret();
            }
        }

        public void ShowServerData(KneeboardServerData data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<KneeboardServerData>(ShowServerData), data);
            }
            else
            {
                DisplayServerData(data);
            }
        }

        public void Clear() => ClearDisplay();
        public void ClearDisplay()
        {
            // Implementa la pulizia del form
            pictureBox.Image = null;
            statusLabel.Text = "Ready";
            // Altri reset necessari...

        }
        private void DisplayServerData(KneeboardServerData data)
        {
            try
            {
                string serverInfo = $"=== VAICOM KNEELBOARD SERVER DATA ===\n" +
                                   $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                   $"=====================================\n\n" +
                                   $"SERVER INFORMATION:\n" +
                                   $"Theater: {data.theater ?? "N/A"}\n" +
                                   $"DCS Version: {data.dcsversion ?? "N/A"}\n" +
                                   $"Aircraft: {data.aircraft ?? "N/A"}\n" +
                                   $"Flight Size: {data.flightsize}\n" +
                                   $"Player: {data.playerusername ?? "N/A"}\n" +
                                   $"Callsign: {data.playercallsign ?? "N/A"}\n" +
                                   $"Coalition: {data.coalition ?? "N/A"}\n" +
                                   $"Sortie: {data.sortie ?? "N/A"}\n" +
                                   $"Task: {data.task ?? "N/A"}\n" +
                                   $"Country: {data.country ?? "N/A"}\n" +
                                   $"Multiplayer: {data.multiplayer}\n\n" +
                                   $"{(string.IsNullOrEmpty(data.missiontitle) ? "" : $"MISSION:\nTitle: {data.missiontitle}\n" +
                                   $"{(string.IsNullOrEmpty(data.missionbriefing) ? "" : $"Briefing: {data.missionbriefing}\n")}" +
                                   $"{(string.IsNullOrEmpty(data.missiondetails) ? "" : $"Details: {data.missiondetails}\n")}\n")}" +
                                   $"=====================================\n" +
                                   $"Waiting for updates...\n";

                AddLogMessage(serverInfo);

                // Aggiorna anche la status bar se vuoi
                statusLabel.Text = $"Server: {data.aircraft} in {data.theater}";
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error displaying server data: {ex.Message}");
            }
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            _manager.PreviousPage();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            _manager.NextPage();
        }

        private void btnNightMode_Click(object sender, EventArgs e)
        {
            _manager.ToggleNightMode();
        }

        private void Manager_GroupsUpdated()
        {
            this.Invoke((MethodInvoker)delegate {
                LoadGroups();
            });
        }

        private void Manager_PageChanged()
        {
            this.Invoke((MethodInvoker)delegate {
                DisplayCurrentPage();
            });
        }

        private void Manager_NightModeToggled()
        {
            this.Invoke((MethodInvoker)delegate {
                UpdateNightModeButton();
                LoadGroups(); // Ricarica per aggiornare indicatori notte
            });
        }

        private void Manager_LogMessageReceived(string message)
        {
            // Invoke è necessario perché viene chiamato da thread diversi
            this.Invoke((MethodInvoker)delegate {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                logTextBox.ScrollToCaret();

                // Se siamo in test mode, mostra anche sulla console
                if (Program.IsTestMode)
                {
                    Console.WriteLine($"[UI] {message}");
                }
            });
        }

        private void UpdateNightModeButton()
        {
            btnNightMode.Text = _manager.NightMode ? "☀️ Day Mode" : "🌙 Night Mode";
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    _manager.NextPage();
                    break;

                case Keys.Left:
                    _manager.PreviousPage();
                    break;

                case Keys.T:
                    _manager.ToggleNightMode();
                    break;

                case Keys.Escape:
                    this.Close();
                    break;

                case Keys.F5:
                    LoadGroups();
                    break;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Rimuovi gli handler
            _manager.GroupsUpdated -= Manager_GroupsUpdated;
            _manager.PageChanged -= Manager_PageChanged;
            _manager.NightModeToggled -= Manager_NightModeToggled;
            _manager.LogMessageReceived -= Manager_LogMessageReceived;

            // Cleanup
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            base.OnFormClosed(e);
        }
    }
}
