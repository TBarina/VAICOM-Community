using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;

namespace VAICOM.KneeboardReceiver
{
    public partial class MainForm : Form, IKneeboardDisplay
    {
        #region TreeView suppurt classes:
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
        #endregion

        private AdvancedKneeboardManager _manager;
        #region ....
        private List<string> _pendingMessages = new List<string>();

        private bool _treeViewCollapsed = false;

        private Button _toggleButton;
        private bool _isPanning = false;
        private Point _panStartPoint = Point.Empty;

        private ContextMenuStrip _pictureBoxContextMenu;
        #endregion

        // 
        /// Constructor
        //
        public MainForm()
        {
            InitializeComponent();
            InitializeTreeViewToggle();
            InitializeZoom();
            InitializePictureBoxContextMenu();
            InitializeConsoleToggle();

            _manager = Program.KneeboardManager;

            // Setup event handlers
            _manager.GroupsUpdated += Manager_GroupsUpdated;
            _manager.PageChanged += Manager_PageChanged;
            _manager.NightModeToggled += Manager_NightModeToggled;
            _manager.LogMessageReceived += Manager_LogMessageReceived;

            // Inizializza i tab
            // groupsTabControl.SelectedIndex = 0;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadGroups();
            UpdateNightModeButton();
        }

        #region Initializers...
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            RemovePictureBoxContextMenu();
            RemoveZoomEventHandlers();

            CleanupTreeViewToggleButton();

            _manager.GroupsUpdated -= Manager_GroupsUpdated;
            _manager.PageChanged -= Manager_PageChanged;
            _manager.NightModeToggled -= Manager_NightModeToggled;
            _manager.LogMessageReceived -= Manager_LogMessageReceived;

            // Cleanup image
            if (kneeboardPictureBox.Image != null)
            {
                kneeboardPictureBox.Image.Dispose();
                kneeboardPictureBox.Image = null;
            }

            base.OnFormClosed(e);
        }
        private void InitializePictureBoxContextMenu()
        {
            _pictureBoxContextMenu = new ContextMenuStrip();

            ToolStripMenuItem zoomInItem = new ToolStripMenuItem("Zoom In", null, (s, e) => HandleZoom(ZOOM_STEP, Point.Empty));
            ToolStripMenuItem zoomOutItem = new ToolStripMenuItem("Zoom Out", null, (s, e) => HandleZoom(-ZOOM_STEP, Point.Empty));
            ToolStripMenuItem resetZoomItem = new ToolStripMenuItem("Reset Zoom", null, (s, e) => ResetZoom());

            _pictureBoxContextMenu.Items.AddRange(new ToolStripItem[] { zoomInItem, zoomOutItem, resetZoomItem });

            kneeboardPictureBox.ContextMenuStrip = _pictureBoxContextMenu;
        }
        private void RemovePictureBoxContextMenu()
        {
            if (_pictureBoxContextMenu != null)
            {
                foreach (ToolStripItem item in _pictureBoxContextMenu.Items)
                {
                    if (item is ToolStripMenuItem menuItem)
                    {
                        menuItem.Click -= null; // This removes all handlers
                    }
                }

                kneeboardPictureBox.ContextMenuStrip = null;
                _pictureBoxContextMenu.Dispose();
                _pictureBoxContextMenu = null;
            }
        }
        private void InitializeTreeViewToggle()
        {
            _toggleButton = new Button
            {
                Text = "▶",
                Width = COLLAPSED_WIDTH,
                Height = 20,
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Visible = true
            };

            _toggleButton.Click += ToggleTreeView;
            this.Controls.Add(_toggleButton);

            UpdateToggleButtonPosition();
            _toggleButton.BringToFront();

            // Inizialize TreeView collapsed
            kneeboardTreeView.Width = COLLAPSED_WIDTH;
            kneeboardTreeView.Visible = true;
            _toggleButton.Text = "◀";
            _treeViewCollapsed = false;
        }
        private void UpdateToggleButtonPosition()
        {
            if (_toggleButton != null && kneeboardTreeView != null)
            {
                _toggleButton.Location = new Point(
                    kneeboardTreeView.Left + 5, // Right - COLLAPSED_WIDTH,
                    kneeboardTreeView.Top + 3
                );
            }
        }
        private void CleanupTreeViewToggleButton()
        {
            if (_toggleButton != null)
            {
                // Removeve handler
                _toggleButton.Click -= ToggleTreeView;

                // Rimuove control
                this.Controls.Remove(_toggleButton);

                // Dispose control
                _toggleButton.Dispose();
                _toggleButton = null;
            }
        }
        private void InitializeZoom()
        {
            kneeboardPictureBox.MouseWheel += HandlePictureBoxMouseWheel;
            kneeboardPictureBox.MouseMove += kneeboardPictureBox_MouseMove;
            kneeboardPictureBox.MouseDown += kneeboardPictureBox_MouseDown;
            kneeboardPictureBox.MouseUp += kneeboardPictureBox_MouseUp;
            ////kneeboardPictureBox.Resize += kneeboardPictureBox_Resize;

            kneeboardPictureBox.Cursor = Cursors.Default;
        }
        private void RemoveZoomEventHandlers()
        {
            kneeboardPictureBox.MouseWheel -= HandlePictureBoxMouseWheel;
            kneeboardPictureBox.MouseMove -= kneeboardPictureBox_MouseMove;
            kneeboardPictureBox.MouseDown -= kneeboardPictureBox_MouseDown;
            kneeboardPictureBox.MouseUp -= kneeboardPictureBox_MouseUp;
            ////kneeboardPictureBox.Resize -= kneeboardPictureBox_Resize;
        }
        private void InitializeConsoleToggle()
        {
            // Inizialize collapsed
            logTextBox.Visible = false;  
            splitContainer2.Panel2MinSize = CONSOLE_COLLAPSED_HEIGHT;
            splitContainer2.SplitterDistance = splitContainer2.Height - CONSOLE_COLLAPSED_HEIGHT;
            toolStripBtnToggleConsole.Text = "Show console";
        }
        #endregion

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
                    // Add node for main group (w/o subgroups)
                    //var mainGroupNode = new TreeNode("(Main Group)")
                    //{
                    //    Tag = new NodeInfo { Type = NodeType.MainGroup, GroupName = group.Name }
                    //};
                    //groupNode.Nodes.Add(mainGroupNode);

                    // Subgroups
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

            //// Add node for Brevity Codes
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
        }
        //private void ConfigureTreeViewImages()
        //{
        //    var imageList = new ImageList();
        //    imageList.Images.Add("group", SystemIcons.Information);
        //    imageList.Images.Add("folder", SystemIcons.Folder);
        //    imageList.Images.Add("folder_open", SystemIcons.FolderOpen);
        //    imageList.Images.Add("book", SystemIcons.Book);
        //    imageList.Images.Add("page", SystemIcons.Document);

        //    kneeboardTreeView.ImageList = imageList;
        //}
        private void ConfigureTreeViewImages()
        {
            var imageList = new ImageList();

            // Usa icone di sistema disponibili invece di quelle inesistenti
            imageList.Images.Add("group", SystemIcons.Information);
            imageList.Images.Add("folder", SystemIcons.Shield); // Alternativw for folder
            imageList.Images.Add("folder_open", SystemIcons.Exclamation); // Alternative for folder open
            imageList.Images.Add("book", SystemIcons.Application);
            imageList.Images.Add("page", SystemIcons.Asterisk);

            kneeboardTreeView.ImageList = imageList;
        }

        public void ShowMainMenu()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowMainMenu));
            }
            else
            {
                LoadGroups(); // Method that loads the list
            }
        }
        public void LoadGroups()
        {
            BuildKneeboardTree();
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
            // Always reset the zoom when page changes
            ResetZoom();

            var currentPage = _manager.GetCurrentPage();
            if (currentPage != null && File.Exists(currentPage.FilePath))
            {
                try
                {
                    // Use a copy to avoid file locking
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

                // Update the status bar
                statusLabel.Text = $"Server: {data.aircraft} in {data.theater}";
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error displaying server data: {ex.Message}");
            }
        }

        public void Clear() => ClearDisplay();
        public void ClearDisplay()
        {
            kneeboardPictureBox.Image = null;
            statusLabel.Text = "Ready";
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

                ////case Keys.R:
                ////    if (_isZoomed)
                ////    {
                ////        ResetZoom();
                ////        e.Handled = true;
                ////    }
                ////    break;

                case Keys.Add:
                case Keys.Oemplus:
                    if (ModifierKeys.HasFlag(Keys.Control))
                    {
                        HandleZoom(ZOOM_STEP, Point.Empty);
                        e.Handled = true;
                    }
                    else
                    {
                        // Espandi nodo selezionato
                        if (kneeboardTreeView.SelectedNode != null)
                            kneeboardTreeView.SelectedNode.Expand();
                        // Espandi nodo selezionato
                    }
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    if (ModifierKeys.HasFlag(Keys.Control))
                    {
                        HandleZoom(-ZOOM_STEP, Point.Empty);
                        e.Handled = true;
                    }
                    else
                    {
                        // Contrai nodo selezionato
                        if (kneeboardTreeView.SelectedNode != null)
                            kneeboardTreeView.SelectedNode.Collapse();
                    }
                    break;

                case Keys.D0:
                    if (ModifierKeys.HasFlag(Keys.Control))
                    {
                        ResetZoom();
                        e.Handled = true;
                    }
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

        //
        #region kneeboardPictureBox zoom methods...
        private void HandleZoom(float zoomDelta, Point mousePosition)
        {
            if (kneeboardPictureBox.Image == null) return;

            if (mousePosition.IsEmpty)
            {
                mousePosition = new Point(kneeboardPictureBox.Width / 2, kneeboardPictureBox.Height / 2);
            }

            kneeboardPictureBox.ApplyZoom(zoomDelta, mousePosition);
            UpdateZoomStatus();
        }
        private void ResetZoom()
        {
            kneeboardPictureBox.ResetView();
            UpdateZoomStatus();
        }
        private void UpdateZoomStatus()
        {
            if (!kneeboardPictureBox.IsInFitMode)
            {
                statusLabel.Text = $"Zoom: {kneeboardPictureBox.CurrentZoom * 100:0}% | Drag to move | Ctrl+MouseWheel to zoom | R to reset";
                kneeboardPictureBox.Cursor = Cursors.Hand;
            }
            else
            {
                var currentPage = _manager.GetCurrentPage();
                if (currentPage != null)
                {
                    statusLabel.Text = $"Page {_manager.CurrentPageIndex + 1} of {_manager.TotalPages} | {Path.GetFileName(currentPage.FilePath)}";
                }
                kneeboardPictureBox.Cursor = Cursors.Default;
            }
        }
        #endregion

        // Helper to find node by NodeInfo
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
        // Helper to find node by path
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

        //
        #region Event Handlers...:
        // 
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

        private void ToggleTreeView(object sender, EventArgs e)
        {
            if (_treeViewCollapsed)
            {
                // Espandi
                splitContainer1.Panel1MinSize = TREEVIEW_EXPANDED_WIDTH;
                splitContainer1.SplitterDistance = TREEVIEW_EXPANDED_WIDTH;

                //kneeboardTreeView.Width = EXPANDED_WIDTH;
                //kneeboardTreeView.Visible = true;
                _toggleButton.Text = "◀";
                _treeViewCollapsed = false;
            }
            else
            {
                // Comprimi
                splitContainer1.Panel1MinSize = TREEVIEW_COLLAPSED_WIDTH;
                splitContainer1.SplitterDistance = TREEVIEW_COLLAPSED_WIDTH;

                //kneeboardTreeView.Width = COLLAPSED_WIDTH;
                //kneeboardTreeView.Visible = false;
                _toggleButton.Text = "▶";
                _treeViewCollapsed = true;
            }

            UpdateToggleButtonPosition();
            _toggleButton.BringToFront();
        }
        private void toolStripBtnToggleConsole_Click(object sender, EventArgs e)
        {
            logTextBox.Visible = !logTextBox.Visible;
            if (!logTextBox.Visible)
            {
                splitContainer2.Panel2MinSize = CONSOLE_COLLAPSED_HEIGHT;
                splitContainer2.SplitterDistance = splitContainer2.Height - CONSOLE_COLLAPSED_HEIGHT;
                toolStripBtnToggleConsole.Text = "Show console";
                //splitContainer2.Panel1Collapsed = true;
            }
            else
            {
                //splitContainer2.Panel1Collapsed = false;  
                splitContainer2.Panel2MinSize = 50;
                splitContainer2.SplitterDistance = splitContainer2.Height - CONSOLE_EXPANDED_HEIGTH;
                toolStripBtnToggleConsole.Text = "Hide console";

            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            UpdateToggleButtonPosition();
        }
        private void btnNightMode_Click(object sender, EventArgs e)
        {
            _manager.ToggleNightMode();
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UpdateToggleButtonPosition();
        }
        
        /// <summary>
        /// ManaTreeView swlwction 
        /// </summary>
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

            // Force refresh
            kneeboardTreeView.Refresh();
            kneeboardTreeView.Invalidate();
            kneeboardTreeView.Update();

            // After selection collapse TreeView
            if (!_treeViewCollapsed)
            {
                splitContainer1.Panel1MinSize = TREEVIEW_COLLAPSED_WIDTH;
                splitContainer1.SplitterDistance = TREEVIEW_COLLAPSED_WIDTH;

                //kneeboardTreeView.Width = COLLAPSED_WIDTH;
                //kneeboardTreeView.Visible = false;
                _toggleButton.Text = "▶";
                _toggleButton.Visible = true;
                _treeViewCollapsed = true;
            }

            LogMessage("TreeView refreshed");
        }
        private void KneeboardTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Doppio click expans/collapses instead of selecting
            if (e.Node.IsExpanded)
                e.Node.Collapse();
            else
                e.Node.Expand();
        }

        private void toolStripBtnZoomOut_Click(object sender, EventArgs e)
        {
            // Calcola il centro del controllo per lo zoom
            Point centerPoint = new Point(kneeboardPictureBox.Width / 2, kneeboardPictureBox.Height / 2);
            HandleZoom(-0.1f, centerPoint);
        }
        private void toolStripBtnZoomIn_Click(object sender, EventArgs e)
        {
            Point centerPoint = new Point(kneeboardPictureBox.Width / 2, kneeboardPictureBox.Height / 2);
            HandleZoom(0.1f, centerPoint);
        }
        private void kneeboardPictureBox_DoubleClick(object sender, EventArgs e)
        {
            ResetZoom();
        }
        private void kneeboardPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !kneeboardPictureBox.IsInFitMode)
            {
                _isPanning = true;
                _panStartPoint = e.Location;
                kneeboardPictureBox.Cursor = Cursors.SizeAll;
            }
        }
        private void kneeboardPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                int deltaX = e.X - _panStartPoint.X;
                int deltaY = e.Y - _panStartPoint.Y;

                kneeboardPictureBox.ApplyPan(deltaX, deltaY);
                _panStartPoint = e.Location; // Aggiorna il punto di partenza per il prossimo movimento
            }
        }
        private void kneeboardPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            _isPanning = false;
            kneeboardPictureBox.Cursor = kneeboardPictureBox.IsInFitMode ? Cursors.Default : Cursors.Hand;
        }
        public void HandlePictureBoxMouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Un delta di 0.1 corrisponde a un 10% di zoom
                float zoomDelta = e.Delta > 0 ? 0.1f : -0.1f;
                HandleZoom(zoomDelta, e.Location);
            }
        }

        private void btnPreviousPage_Click(object sender, EventArgs e)
        {
            _manager.PreviousPage();
        }
        private void btnNextPage_Click(object sender, EventArgs e)
        {
            _manager.NextPage();
        }
        #endregion
    }
}
