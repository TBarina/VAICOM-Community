namespace VAICOM.KneeboardReceiver
{
    partial class MainForm
    {
        private const int COLLAPSED_WIDTH = 20;
        private const int EXPANDED_WIDTH = 250; 
        private const int TREEVIEW_EXPANDED_WIDTH = 200;
        private const int TREEVIEW_COLLAPSED_WIDTH =  0;
        private const int CONSOLE_EXPANDED_HEIGTH = 150;
        private const int CONSOLE_COLLAPSED_HEIGHT =  0;
        private const float ZOOM_MIN = 0.5f;
        private const float ZOOM_MAX = 2.0f;
        private const float ZOOM_STEP = 0.025f;

        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton toolStripBtnZoomOut;
        private System.Windows.Forms.ToolStripButton toolStripBtnZoomIn;
        private System.Windows.Forms.ToolStripButton toolStripBtnToggleConsole;
        private System.Windows.Forms.ToolStripButton btnNightMode;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        //private PictureBox kneeboardPictureBox;
        private TreeView kneeboardTreeView;
        private TextBox logTextBox;
        private ZoomPictureBox kneeboardPictureBox;
        private Button btnNextPage;
        private Button btnPreviousPage;
        private ContextMenuStrip treeContextMenu;
        private ToolStripMenuItem menuItemRefresh;
        private ToolStripMenuItem menuItemExpandAll;
        private ToolStripMenuItem menuItemCollapseAll;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            statusStrip1 = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            toolStrip1 = new ToolStrip();
            toolStripSeparator1 = new ToolStripSeparator();
            btnNightMode = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripBtnToggleConsole = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStripBtnZoomIn = new ToolStripButton();
            toolStripBtnZoomOut = new ToolStripButton();
            toolStripLabel1 = new ToolStripLabel();
            splitContainer1 = new SplitContainer();
            kneeboardTreeView = new TreeView();
            treeContextMenu = new ContextMenuStrip(components);
            menuItemRefresh = new ToolStripMenuItem();
            menuItemExpandAll = new ToolStripMenuItem();
            menuItemCollapseAll = new ToolStripMenuItem();
            btnNextPage = new Button();
            btnPreviousPage = new Button();
            kneeboardPictureBox = new ZoomPictureBox();
            splitContainer2 = new SplitContainer();
            logTextBox = new TextBox();
            statusStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            treeContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)kneeboardPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip1.Location = new Point(0, 707);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 16, 0);
            statusStrip1.Size = new Size(1008, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(991, 17);
            statusLabel.Spring = true;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStrip1
            // 
            toolStrip1.BackColor = SystemColors.ControlDark;
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripSeparator1, btnNightMode, toolStripSeparator3, toolStripBtnToggleConsole, toolStripSeparator2, toolStripBtnZoomIn, toolStripBtnZoomOut, toolStripLabel1 });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.RightToLeft = RightToLeft.Yes;
            toolStrip1.Size = new Size(1008, 25);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // btnNightMode
            // 
            btnNightMode.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNightMode.ImageTransparentColor = Color.Magenta;
            btnNightMode.Name = "btnNightMode";
            btnNightMode.Size = new Size(90, 22);
            btnNightMode.Text = "🌙 Night Mode";
            btnNightMode.Click += btnNightMode_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 25);
            // 
            // toolStripBtnToggleConsole
            // 
            toolStripBtnToggleConsole.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnToggleConsole.Image = (Image)resources.GetObject("toolStripBtnToggleConsole.Image");
            toolStripBtnToggleConsole.ImageTransparentColor = Color.Magenta;
            toolStripBtnToggleConsole.Name = "toolStripBtnToggleConsole";
            toolStripBtnToggleConsole.Size = new Size(84, 22);
            toolStripBtnToggleConsole.Text = "Show console";
            toolStripBtnToggleConsole.Click += toolStripBtnToggleConsole_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // toolStripBtnZoomIn
            // 
            toolStripBtnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnZoomIn.Image = (Image)resources.GetObject("toolStripBtnZoomIn.Image");
            toolStripBtnZoomIn.ImageTransparentColor = Color.Magenta;
            toolStripBtnZoomIn.Name = "toolStripBtnZoomIn";
            toolStripBtnZoomIn.Size = new Size(23, 22);
            toolStripBtnZoomIn.Text = "+";
            toolStripBtnZoomIn.ToolTipText = "Zoom in";
            toolStripBtnZoomIn.Click += toolStripBtnZoomIn_Click;
            // 
            // toolStripBtnZoomOut
            // 
            toolStripBtnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnZoomOut.Image = (Image)resources.GetObject("toolStripBtnZoomOut.Image");
            toolStripBtnZoomOut.ImageTransparentColor = Color.Magenta;
            toolStripBtnZoomOut.Name = "toolStripBtnZoomOut";
            toolStripBtnZoomOut.Size = new Size(23, 22);
            toolStripBtnZoomOut.Text = "-";
            toolStripBtnZoomOut.ToolTipText = "Zoom out";
            toolStripBtnZoomOut.Click += toolStripBtnZoomOut_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new Size(39, 22);
            toolStripLabel1.Text = "Zoom";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(3, 2, 3, 2);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(kneeboardTreeView);
            splitContainer1.Panel1MinSize = 0;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(btnNextPage);
            splitContainer1.Panel2.Controls.Add(btnPreviousPage);
            splitContainer1.Panel2.Controls.Add(kneeboardPictureBox);
            splitContainer1.Panel2MinSize = 576;
            splitContainer1.Size = new Size(1008, 652);
            splitContainer1.SplitterDistance = 226;
            splitContainer1.SplitterWidth = 2;
            splitContainer1.TabIndex = 1;
            splitContainer1.SplitterMoved += splitContainer1_SplitterMoved;
            // 
            // kneeboardTreeView
            // 
            kneeboardTreeView.BackColor = SystemColors.ControlDark;
            kneeboardTreeView.ContextMenuStrip = treeContextMenu;
            kneeboardTreeView.Dock = DockStyle.Fill;
            kneeboardTreeView.Font = new Font("Consolas", 11.25F);
            kneeboardTreeView.Location = new Point(0, 0);
            kneeboardTreeView.Name = "kneeboardTreeView";
            kneeboardTreeView.Size = new Size(226, 652);
            kneeboardTreeView.TabIndex = 0;
            kneeboardTreeView.AfterSelect += KneeboardTreeView_AfterSelect;
            kneeboardTreeView.NodeMouseDoubleClick += KneeboardTreeView_NodeMouseDoubleClick;
            // 
            // treeContextMenu
            // 
            treeContextMenu.Items.AddRange(new ToolStripItem[] { menuItemRefresh, menuItemExpandAll, menuItemCollapseAll });
            treeContextMenu.Name = "treeContextMenu";
            treeContextMenu.Size = new Size(137, 70);
            // 
            // menuItemRefresh
            // 
            menuItemRefresh.Name = "menuItemRefresh";
            menuItemRefresh.Size = new Size(136, 22);
            menuItemRefresh.Text = "Refresh";
            menuItemRefresh.Click += menuItemRefresh_Click;
            // 
            // menuItemExpandAll
            // 
            menuItemExpandAll.Name = "menuItemExpandAll";
            menuItemExpandAll.Size = new Size(136, 22);
            menuItemExpandAll.Text = "Expand All";
            menuItemExpandAll.Click += menuItemExpandAll_Click;
            // 
            // menuItemCollapseAll
            // 
            menuItemCollapseAll.Name = "menuItemCollapseAll";
            menuItemCollapseAll.Size = new Size(136, 22);
            menuItemCollapseAll.Text = "Collapse All";
            menuItemCollapseAll.Click += menuItemCollapseAll_Click;
            // 
            // btnNextPage
            // 
            btnNextPage.BackColor = SystemColors.ControlDark;
            btnNextPage.Dock = DockStyle.Right;
            btnNextPage.FlatAppearance.BorderSize = 0;
            btnNextPage.FlatStyle = FlatStyle.Flat;
            btnNextPage.Font = new Font("Arial Black", 14F);
            btnNextPage.ForeColor = SystemColors.ButtonFace;
            btnNextPage.Location = new Point(743, 0);
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Size = new Size(37, 652);
            btnNextPage.TabIndex = 2;
            btnNextPage.Text = ">";
            btnNextPage.UseVisualStyleBackColor = false;
            btnNextPage.Click += btnNextPage_Click;
            // 
            // btnPreviousPage
            // 
            btnPreviousPage.BackColor = SystemColors.ControlDark;
            btnPreviousPage.Dock = DockStyle.Left;
            btnPreviousPage.FlatAppearance.BorderSize = 0;
            btnPreviousPage.FlatStyle = FlatStyle.Flat;
            btnPreviousPage.Font = new Font("Arial Black", 14F);
            btnPreviousPage.ForeColor = SystemColors.ButtonFace;
            btnPreviousPage.Location = new Point(0, 0);
            btnPreviousPage.Name = "btnPreviousPage";
            btnPreviousPage.Size = new Size(37, 652);
            btnPreviousPage.TabIndex = 1;
            btnPreviousPage.Text = "<";
            btnPreviousPage.UseVisualStyleBackColor = false;
            btnPreviousPage.Click += btnPreviousPage_Click;
            // 
            // kneeboardPictureBox
            // 
            kneeboardPictureBox.BackColor = Color.FromArgb(45, 45, 45);
            kneeboardPictureBox.Dock = DockStyle.Fill;
            kneeboardPictureBox.ErrorImage = null;
            kneeboardPictureBox.Image = null;
            kneeboardPictureBox.InitialImage = null;
            kneeboardPictureBox.Location = new Point(0, 0);
            kneeboardPictureBox.Margin = new Padding(4, 3, 4, 3);
            kneeboardPictureBox.Name = "kneeboardPictureBox";
            kneeboardPictureBox.Size = new Size(780, 652);
            kneeboardPictureBox.TabIndex = 0;
            kneeboardPictureBox.TabStop = false;
            kneeboardPictureBox.DoubleClick += kneeboardPictureBox_DoubleClick;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 25);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(splitContainer1);
            splitContainer2.Panel1MinSize = 100;
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.BackColor = SystemColors.ButtonFace;
            splitContainer2.Panel2.Controls.Add(logTextBox);
            splitContainer2.Panel2MinSize = 0;
            splitContainer2.Size = new Size(1008, 682);
            splitContainer2.SplitterDistance = 652;
            splitContainer2.SplitterWidth = 2;
            splitContainer2.TabIndex = 3;
            // 
            // logTextBox
            // 
            logTextBox.BackColor = Color.Black;
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Font = new Font("Consolas", 9F);
            logTextBox.ForeColor = Color.LightGray;
            logTextBox.Location = new Point(0, 0);
            logTextBox.Margin = new Padding(4, 3, 4, 3);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.Size = new Size(1008, 28);
            logTextBox.TabIndex = 6;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(1008, 729);
            Controls.Add(splitContainer2);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(768, 576);
            Name = "MainForm";
            SizeGripStyle = SizeGripStyle.Show;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VAICOM Kneeboard Manager";
            TopMost = true;
            Load += MainForm_Load;
            KeyDown += MainForm_KeyDown;
            Resize += MainForm_Resize;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            treeContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)kneeboardPictureBox).EndInit();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
    }
}