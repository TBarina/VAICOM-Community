namespace VAICOM.KneeboardReceiver
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnPrevious;
        private System.Windows.Forms.ToolStripButton btnNext;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnNightMode;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer1;
        private PictureBox kneeboardPictureBox;
        private TextBox logTextBox;

        private System.Windows.Forms.TabControl groupsTabControl;
        private System.Windows.Forms.TabPage mainGroupsTab;
        //private ListBox groupListBox;
        //private System.Windows.Forms.TabPage subGroupsTab;
        //private System.Windows.Forms.ListBox subGroupListBox;
        //private System.Windows.Forms.TabPage brevityCodesTab;
        //private System.Windows.Forms.ListView brevityCodesListView;
        //private System.Windows.Forms.ColumnHeader columnHeader1;
        //private System.Windows.Forms.ColumnHeader columnHeader2;

        private TreeView kneeboardTreeView;
        private ContextMenuStrip treeContextMenu;

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
            statusStrip1 = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            toolStrip1 = new ToolStrip();
            btnPrevious = new ToolStripButton();
            btnNext = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            btnNightMode = new ToolStripButton();
            splitContainer2 = new SplitContainer();
            splitContainer1 = new SplitContainer();
            groupsTabControl = new TabControl();
            mainGroupsTab = new TabPage();
            kneeboardTreeView = new TreeView();
            treeContextMenu = new ContextMenuStrip(components);
            menuItemRefresh = new ToolStripMenuItem();
            menuItemExpandAll = new ToolStripMenuItem();
            menuItemCollapseAll = new ToolStripMenuItem();
            kneeboardPictureBox = new PictureBox();
            logTextBox = new TextBox();
            statusStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            groupsTabControl.SuspendLayout();
            mainGroupsTab.SuspendLayout();
            treeContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)kneeboardPictureBox).BeginInit();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip1.Location = new Point(0, 651);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 16, 0);
            statusStrip1.Size = new Size(1148, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(1131, 17);
            statusLabel.Spring = true;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { btnPrevious, btnNext, toolStripSeparator1, btnNightMode });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1148, 25);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnPrevious
            // 
            btnPrevious.AutoSize = false;
            btnPrevious.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPrevious.ImageTransparentColor = Color.Magenta;
            btnPrevious.Name = "btnPrevious";
            btnPrevious.Size = new Size(75, 22);
            btnPrevious.Text = "◀ Previous";
            btnPrevious.Click += btnPrevious_Click;
            // 
            // btnNext
            // 
            btnNext.AutoSize = false;
            btnNext.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNext.ImageTransparentColor = Color.Magenta;
            btnNext.Name = "btnNext";
            btnNext.RightToLeftAutoMirrorImage = true;
            btnNext.Size = new Size(75, 22);
            btnNext.Text = "Next ▶";
            btnNext.Click += btnNext_Click;
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
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(logTextBox);
            splitContainer2.Size = new Size(1148, 626);
            splitContainer2.SplitterDistance = 544;
            splitContainer2.TabIndex = 3;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(4, 3, 4, 3);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(groupsTabControl);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(kneeboardPictureBox);
            splitContainer1.Size = new Size(1148, 544);
            splitContainer1.SplitterDistance = 259;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // groupsTabControl
            // 
            groupsTabControl.Controls.Add(mainGroupsTab);
            groupsTabControl.Dock = DockStyle.Fill;
            groupsTabControl.Location = new Point(0, 0);
            groupsTabControl.Name = "groupsTabControl";
            groupsTabControl.SelectedIndex = 0;
            groupsTabControl.Size = new Size(259, 544);
            groupsTabControl.TabIndex = 0;
            // 
            // mainGroupsTab
            // 
            mainGroupsTab.Controls.Add(kneeboardTreeView);
            mainGroupsTab.Location = new Point(4, 24);
            mainGroupsTab.Name = "mainGroupsTab";
            mainGroupsTab.Padding = new Padding(3);
            mainGroupsTab.Size = new Size(251, 516);
            mainGroupsTab.TabIndex = 0;
            mainGroupsTab.Text = "Groups";
            mainGroupsTab.UseVisualStyleBackColor = true;
            // 
            // kneeboardTreeView
            // 
            kneeboardTreeView.ContextMenuStrip = treeContextMenu;
            kneeboardTreeView.Dock = DockStyle.Fill;
            kneeboardTreeView.Font = new Font("Consolas", 11.25F);
            kneeboardTreeView.Location = new Point(3, 3);
            kneeboardTreeView.Name = "kneeboardTreeView";
            kneeboardTreeView.Size = new Size(245, 510);
            kneeboardTreeView.TabIndex = 0;
            kneeboardTreeView.AfterSelect += KneeboardTreeView_AfterSelect;
            kneeboardTreeView.NodeMouseDoubleClick += KneeboardTreeView_NodeMouseDoubleClick;
            // 
            // treeContextMenu
            // 
            treeContextMenu.Items.AddRange(new ToolStripItem[] { menuItemRefresh, menuItemExpandAll, menuItemCollapseAll });
            treeContextMenu.Name = "treeContextMenu";
            treeContextMenu.Size = new Size(181, 92);
            // 
            // menuItemRefresh
            // 
            menuItemRefresh.Name = "menuItemRefresh";
            menuItemRefresh.Size = new Size(180, 22);
            menuItemRefresh.Text = "Refresh";
            menuItemRefresh.Click += menuItemRefresh_Click;
            // 
            // menuItemExpandAll
            // 
            menuItemExpandAll.Name = "menuItemExpandAll";
            menuItemExpandAll.Size = new Size(180, 22);
            menuItemExpandAll.Text = "Expand All";
            menuItemExpandAll.Click += menuItemExpandAll_Click;
            // 
            // menuItemCollapseAll
            // 
            menuItemCollapseAll.Name = "menuItemCollapseAll";
            menuItemCollapseAll.Size = new Size(180, 22);
            menuItemCollapseAll.Text = "Collapse All";
            menuItemCollapseAll.Click += menuItemCollapseAll_Click;
            // 
            // kneeboardPictureBox
            // 
            kneeboardPictureBox.BackColor = Color.FromArgb(45, 45, 45);
            kneeboardPictureBox.Dock = DockStyle.Fill;
            kneeboardPictureBox.Location = new Point(0, 0);
            kneeboardPictureBox.Margin = new Padding(4, 3, 4, 3);
            kneeboardPictureBox.Name = "kneeboardPictureBox";
            kneeboardPictureBox.Size = new Size(884, 544);
            kneeboardPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            kneeboardPictureBox.TabIndex = 0;
            kneeboardPictureBox.TabStop = false;
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
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(1148, 78);
            logTextBox.TabIndex = 4;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1148, 673);
            Controls.Add(splitContainer2);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(931, 686);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VAICOM Kneeboard Manager";
            Load += MainForm_Load;
            KeyDown += MainForm_KeyDown;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            groupsTabControl.ResumeLayout(false);
            mainGroupsTab.ResumeLayout(false);
            treeContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)kneeboardPictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private ToolStripMenuItem menuItemRefresh;
        private ToolStripMenuItem menuItemExpandAll;
        private ToolStripMenuItem menuItemCollapseAll;
    }
}