using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VAICOM.KneeboardReceiver
{
    public partial class MainForm : Form, IKneeboardDisplay
    {
        private AdvancedKneeboardManager _manager;
        private List<string> _pendingMessages = new List<string>();

        public MainForm()
        {
            InitializeComponent();

            _manager = Program.KneeboardManager;

            // Setup event handlers
            _manager.GroupsUpdated += Manager_GroupsUpdated;
            _manager.PageChanged += Manager_PageChanged;
            _manager.NightModeToggled += Manager_NightModeToggled;
            _manager.LogMessageReceived += Manager_LogMessageReceived;

            // Inizializza i tab
            groupsTabControl.SelectedIndex = 0;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadGroups();
            UpdateNightModeButton();
        }

        private void BuildKneeboardTree()
        {
            kneeboardTreeView.Nodes.Clear();
            var groups = _manager.GetAvailableGroups();

            // Nodo radice con tipo Root
            var rootNode = new TreeNode("📁 Kneeboard Groups")
            {
                Tag = new NodeInfo { Type = NodeType.Root } // Usa NodeType.Root
            };

            foreach (var group in groups.OrderBy(g => g.DisplayName))
            {
                var groupNode = new TreeNode(group.DisplayName)
                {
                    Tag = new NodeInfo { Type = NodeType.Group, GroupName = group.Name }
                };

                // Aggiungi sottogruppi
                if (group.SubGroups.Count > 0)
                {
                    // Nodo per il gruppo principale (senza sottogruppo)
                    var mainGroupNode = new TreeNode("(Main Group)")
                    {
                        Tag = new NodeInfo { Type = NodeType.MainGroup, GroupName = group.Name }
                    };
                    groupNode.Nodes.Add(mainGroupNode);

                    // Sottogruppi
                    foreach (var subGroup in group.SubGroups.OrderBy(s => s))
                    {
                        var subGroupNode = new TreeNode(subGroup)
                        {
                            Tag = new NodeInfo
                            {
                                Type = NodeType.SubGroup,
                                GroupName = group.Name,
                                SubGroupName = subGroup
                            }
                        };
                        groupNode.Nodes.Add(subGroupNode);
                    }
                }

                rootNode.Nodes.Add(groupNode);
            }

            //// Aggiungi nodo per Brevity Codes
            //var brevityNode = new TreeNode("Brevity Codes")
            //{
            //    Tag = new NodeInfo { Type = NodeType.BrevityRoot }
            //};

            //for (char c = 'A'; c <= 'Z'; c++)
            //{
            //    var letterNode = new TreeNode(c.ToString())
            //    {
            //        Tag = new NodeInfo
            //        {
            //            Type = NodeType.BrevityLetter,
            //            GroupName = "BrevityCodes",
            //            SubGroupName = c.ToString()
            //        }
            //    };
            //    brevityNode.Nodes.Add(letterNode);
            //}
            //rootNode.Nodes.Add(brevityNode);
            
            kneeboardTreeView.Nodes.Add(rootNode);
            rootNode.Expand();
        }        //private void ConfigureTreeViewImages()
        //{
        //    var imageList = new ImageList();
        //    imageList.Images.Add("group", SystemIcons.Information); // Icona gruppo
        //    imageList.Images.Add("folder", SystemIcons.Folder); // Icona cartella
        //    imageList.Images.Add("folder_open", SystemIcons.FolderOpen); // Cartella aperta
        //    imageList.Images.Add("book", SystemIcons.Book); // Libro
        //    imageList.Images.Add("page", SystemIcons.Document); // Pagina

        //    kneeboardTreeView.ImageList = imageList;
        //}
        private void ConfigureTreeViewImages()
        {
            var imageList = new ImageList();

            // Usa icone di sistema disponibili invece di quelle inesistenti
            imageList.Images.Add("group", SystemIcons.Information); // Icona gruppo
            imageList.Images.Add("folder", SystemIcons.Shield); // Usa Shield come folder (alternativa)
            imageList.Images.Add("folder_open", SystemIcons.Exclamation); // Alternativa per folder open
            imageList.Images.Add("book", SystemIcons.Application); // Libro
            imageList.Images.Add("page", SystemIcons.Asterisk); // Pagina

            kneeboardTreeView.ImageList = imageList;
        }

        private void KneeboardTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is NodeInfo nodeInfo)
            {
                switch (nodeInfo.Type)
                {
                    case NodeType.Root:
                        // Nessuna azione per la radice
                        break;

                    case NodeType.Group:
                        // Gruppo senza sottogruppi - carica direttamente
                        _manager.LoadGroup(nodeInfo.GroupName);
                        DisplayCurrentPage();
                        break;

                    case NodeType.MainGroup:
                        // Gruppo principale (senza sottogruppo)
                        _manager.LoadGroup(nodeInfo.GroupName);
                        DisplayCurrentPage();
                        break;

                    case NodeType.SubGroup:
                        // Sottogruppo specifico
                        _manager.LoadGroup(nodeInfo.GroupName, nodeInfo.SubGroupName);
                        DisplayCurrentPage();
                        break;

                    case NodeType.BrevityRoot:
                        // Nessuna azione per la radice brevity
                        break;

                    case NodeType.BrevityLetter:
                        // Lettera brevity code
                        _manager.LoadGroup(nodeInfo.GroupName, nodeInfo.SubGroupName);
                        DisplayCurrentPage();
                        break;
                }
            }


            // Forza il refresh
            kneeboardTreeView.Refresh();
            kneeboardTreeView.Invalidate();
            kneeboardTreeView.Update();

            LogMessage("TreeView refreshed");
        }
        private void KneeboardTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Doppio click espande/contrae invece di selezionare
            if (e.Node.IsExpanded)
                e.Node.Collapse();
            else
                e.Node.Expand();
        }

        // Classi di supporto per il TreeView
        private enum NodeType
        {
            Root,           // Nodo radice "Kneeboard Groups"
            Group,          // Gruppo principale (es: "F-16C Procedures")
            MainGroup,      // Sottogruppo principale "(Main Group)" 
            SubGroup,       // Sottogruppo specifico (es: "Emergency")
            BrevityRoot,    // Radice brevity codes "Brevity Codes"
            BrevityLetter   // Lettera brevity code (es: "A")
        }

        private class NodeInfo
        {
            public NodeType Type { get; set; }
            public string GroupName { get; set; }
            public string SubGroupName { get; set; }
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
            BuildKneeboardTree();

            // Inizializza i brevity codes
            // InitializeBrevityCodes();
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
                        kneeboardPictureBox.Image = new Bitmap(tempImage);
                    }

                    statusLabel.Text = $"Page {_manager.CurrentPageIndex + 1} of {_manager.TotalPages} | {Path.GetFileName(currentPage.FilePath)}";
                }
                catch (Exception ex)
                {
                    kneeboardPictureBox.Image = null;
                    statusLabel.Text = $"Error loading image: {ex.Message}";
                }
            }
            else
            {
                kneeboardPictureBox.Image = null;
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
            kneeboardPictureBox.Image = null;
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
            this.Invoke((MethodInvoker)delegate
            {
                // Salva lo stato corrente della selezione nel TreeView
                TreeNode selectedNode = kneeboardTreeView.SelectedNode;
                NodeInfo selectedNodeInfo = selectedNode?.Tag as NodeInfo;

                // Ricarica l'albero
                BuildKneeboardTree();

                // Ripristina la selezione precedente se possibile
                if (selectedNodeInfo != null)
                {
                    TreeNode nodeToSelect = FindNodeByInfo(selectedNodeInfo);
                    if (nodeToSelect != null)
                    {
                        kneeboardTreeView.SelectedNode = nodeToSelect;
                        nodeToSelect.EnsureVisible();

                        // Se era un nodo selezionabile, ricarica anche i contenuti
                        if (selectedNodeInfo.Type == NodeType.Group ||
                            selectedNodeInfo.Type == NodeType.MainGroup ||
                            selectedNodeInfo.Type == NodeType.SubGroup ||
                            selectedNodeInfo.Type == NodeType.BrevityLetter)
                        {
                            KneeboardTreeView_AfterSelect(this, new TreeViewEventArgs(nodeToSelect));
                        }
                    }
                }
                else
                {
                    // Se non c'era selezione precedente, seleziona il primo nodo
                    if (kneeboardTreeView.Nodes.Count > 0 && kneeboardTreeView.Nodes[0].Nodes.Count > 0)
                    {
                        kneeboardTreeView.SelectedNode = kneeboardTreeView.Nodes[0].Nodes[0];
                    }
                }
            });
        }

        // Metodo helper per trovare un nodo basato su NodeInfo
        private TreeNode FindNodeByInfo(NodeInfo targetInfo)
        {
            foreach (TreeNode node in kneeboardTreeView.Nodes)
            {
                var foundNode = FindNodeByInfoRecursive(node, targetInfo);
                if (foundNode != null) return foundNode;
            }
            return null;
        }

        private TreeNode FindNodeByInfoRecursive(TreeNode currentNode, NodeInfo targetInfo)
        {
            if (currentNode.Tag is NodeInfo currentInfo)
            {
                // Ora NodeType.Root è definito
                if (currentInfo.Type == targetInfo.Type &&
                    currentInfo.GroupName == targetInfo.GroupName &&
                    currentInfo.SubGroupName == targetInfo.SubGroupName)
                {
                    return currentNode;
                }
            }

            foreach (TreeNode childNode in currentNode.Nodes)
            {
                var foundNode = FindNodeByInfoRecursive(childNode, targetInfo);
                if (foundNode != null) return foundNode;
            }

            return null;
        }

        // Metodo helper per trovare un nodo per percorso
        private TreeNode FindNodeByPath(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                if (path.StartsWith(node.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    var found = FindNodeByPath(node.Nodes, path);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void Manager_PageChanged()
        {
            this.Invoke((MethodInvoker)delegate
            {
                DisplayCurrentPage();
            });
        }

        private void Manager_NightModeToggled()
        {
            this.Invoke((MethodInvoker)delegate
            {
                UpdateNightModeButton();
                LoadGroups(); // Ricarica per aggiornare indicatori notte
            });
        }

        private void Manager_LogMessageReceived(string message)
        {
            // Controlla se l'handle è stato creato e se Invoke è necessario
            if (this.IsHandleCreated)
            {
                if (this.InvokeRequired)
                {
                    // Invoke è necessario perché viene chiamato da thread diversi
                    this.Invoke((MethodInvoker)delegate
                    {
                        logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                        logTextBox.ScrollToCaret();

                        // Se siamo in test mode, mostra anche sulla console
                        if (Program.IsTestMode)
                        {
                            Console.WriteLine($"[UI] {message}");
                        }
                    });
                }
                else
                {
                    // Se siamo già sul thread UI
                    logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                    logTextBox.ScrollToCaret();

                    if (Program.IsTestMode)
                    {
                        Console.WriteLine($"[UI] {message}");
                    }
                }
            }
            else
            {
                // Handle non ancora creato, memorizza i messaggi o logga su console
                if (Program.IsTestMode)
                {
                    Console.WriteLine($"[DELAYED UI] {message}");
                }
                // Opzionale: memorizza in una lista per visualizzare dopo
                _pendingMessages.Add(message);
            }
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
                    LoadGroups(); // Ricarica l'albero
                    break;

                case Keys.Enter:
                    // Enter per selezionare il nodo corrente
                    if (kneeboardTreeView.SelectedNode != null)
                    {
                        KneeboardTreeView_AfterSelect(this,
                            new TreeViewEventArgs(kneeboardTreeView.SelectedNode));
                    }
                    break;

                case Keys.Add:
                    // Espandi nodo selezionato
                    if (kneeboardTreeView.SelectedNode != null)
                        kneeboardTreeView.SelectedNode.Expand();
                    break;

                case Keys.Subtract:
                    // Contrai nodo selezionato
                    if (kneeboardTreeView.SelectedNode != null)
                        kneeboardTreeView.SelectedNode.Collapse();
                    break;

                case Keys.Multiply:
                    // Espandi tutto
                    kneeboardTreeView.ExpandAll();
                    break;

                case Keys.Divide:
                    // Contrai tutto
                    kneeboardTreeView.CollapseAll();
                    break;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            foreach (var message in _pendingMessages)
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
            logTextBox.ScrollToCaret();
            _pendingMessages.Clear();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Rimuovi gli handler
            _manager.GroupsUpdated -= Manager_GroupsUpdated;
            _manager.PageChanged -= Manager_PageChanged;
            _manager.NightModeToggled -= Manager_NightModeToggled;
            _manager.LogMessageReceived -= Manager_LogMessageReceived;

            // Cleanup
            if (kneeboardPictureBox.Image != null)
            {
                kneeboardPictureBox.Image.Dispose();
                kneeboardPictureBox.Image = null;
            }

            base.OnFormClosed(e);
        }

        private void UpdatePageInfo()
        {
            if (_manager.TotalPages > 0)
            {
                //lblPageInfo.Text = $"Page: {_manager.CurrentPageIndex + 1}/{_manager.TotalPages}";

                // Mostra l'immagine della pagina corrente
                var currentPage = _manager.GetCurrentPage();
                if (currentPage != null && File.Exists(currentPage.FilePath))
                {
                    try
                    {
                        kneeboardPictureBox.Image = Image.FromFile(currentPage.FilePath);
                        kneeboardPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error loading image: {ex.Message}");
                    }
                }
            }
        }

        private void menuItemExpandAll_Click(object sender, EventArgs e)
        {
            LogMessage("Expanding all tree nodes...");
            kneeboardTreeView.BeginUpdate();
            try
            {
                kneeboardTreeView.ExpandAll();
            }
            finally
            {
                kneeboardTreeView.EndUpdate();
            }
        }

        private void menuItemCollapseAll_Click(object sender, EventArgs e)
        {
            LogMessage("Collapsing all tree nodes...");
            kneeboardTreeView.BeginUpdate();
            try
            {
                kneeboardTreeView.CollapseAll();

                // Lascia espansa solo la radice per migliore UX
                if (kneeboardTreeView.Nodes.Count > 0)
                {
                    kneeboardTreeView.Nodes[0].Expand();
                }
            }
            finally
            {
                kneeboardTreeView.EndUpdate();
            }
        }

        private void menuItemRefresh_Click(object sender, EventArgs e)
        {
            LogMessage("Refresh clicked - reloading groups...");
            LoadGroups(); // Ricarica tutti i gruppi
            kneeboardTreeView.ExpandAll(); // Espandi tutto per visibilità
        }
    }
}
