namespace CtrlCV
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView listViewSlots;
        private System.Windows.Forms.ColumnHeader colSlot;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colPreview;
        private System.Windows.Forms.ImageList imageListThumbs;

        private System.Windows.Forms.TableLayoutPanel panelBottom;
        private System.Windows.Forms.FlowLayoutPanel flowButtons;
        private System.Windows.Forms.Button btnClearAll;
        private System.Windows.Forms.Button btnRemoveSelected;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Label lblStatus;

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuTray;
        private System.Windows.Forms.ToolStripMenuItem menuShow;
        private System.Windows.Forms.ToolStripMenuItem menuScreenshot;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuClearAll;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuCheckForUpdates;
        private System.Windows.Forms.ToolStripMenuItem menuToggleWidget;

        private System.Windows.Forms.ContextMenuStrip screenshotMenu;
        private System.Windows.Forms.ToolStripMenuItem menuFullScreen;
        private System.Windows.Forms.ToolStripMenuItem menuActiveWindow;
        private System.Windows.Forms.ToolStripMenuItem menuSelectRegion;

        private System.Windows.Forms.ToolStripMenuItem menuTrayFullScreen;
        private System.Windows.Forms.ToolStripMenuItem menuTrayActiveWindow;
        private System.Windows.Forms.ToolStripMenuItem menuTraySelectRegion;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;

        private System.Windows.Forms.ContextMenuStrip contextMenuSlot;
        private System.Windows.Forms.ToolStripMenuItem menuSlotPin;
        private System.Windows.Forms.ToolStripMenuItem menuSlotExtractText;
        private System.Windows.Forms.ToolStripMenuItem menuSlotRemove;
        private System.Windows.Forms.ToolStripMenuItem menuSlotClearAll;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                CleanupResources();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.imageListThumbs = new System.Windows.Forms.ImageList(this.components);
            this.colSlot = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colPreview = new System.Windows.Forms.ColumnHeader();
            this.listViewSlots = new System.Windows.Forms.ListView();
            this.btnClearAll = new System.Windows.Forms.Button();
            this.btnRemoveSelected = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.panelBottom = new System.Windows.Forms.TableLayoutPanel();
            this.menuFullScreen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuActiveWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSelectRegion = new System.Windows.Forms.ToolStripMenuItem();
            this.screenshotMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuShow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTrayFullScreen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTrayActiveWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTraySelectRegion = new System.Windows.Forms.ToolStripMenuItem();
            this.menuScreenshot = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuClearAll = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCheckForUpdates = new System.Windows.Forms.ToolStripMenuItem();
            this.menuToggleWidget = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.contextMenuTray = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.panelBottom.SuspendLayout();
            this.screenshotMenu.SuspendLayout();
            this.contextMenuTray.SuspendLayout();
            this.SuspendLayout();
            // 
            // imageListThumbs
            // 
            this.imageListThumbs.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListThumbs.ImageSize = new System.Drawing.Size(32, 32);
            this.imageListThumbs.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // colSlot
            // 
            this.colSlot.Text = "Slot";
            this.colSlot.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.colSlot.Width = -2;
            // 
            // colType
            // 
            this.colType.Text = "Type";
            this.colType.Width = -2;
            // 
            // colPreview
            // 
            this.colPreview.Text = "Preview";
            this.colPreview.Width = -2;
            // 
            // menuSlotPin
            // 
            this.menuSlotPin = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSlotPin.Name = "menuSlotPin";
            this.menuSlotPin.Text = "Pin";
            this.menuSlotPin.Click += new System.EventHandler(this.MenuSlotPin_Click);
            // 
            // menuSlotExtractText
            // 
            this.menuSlotExtractText = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSlotExtractText.Name = "menuSlotExtractText";
            this.menuSlotExtractText.Text = "Extract Text";
            this.menuSlotExtractText.Click += new System.EventHandler(this.MenuSlotExtractText_Click);
            // 
            // menuSlotRemove
            // 
            this.menuSlotRemove = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSlotRemove.Name = "menuSlotRemove";
            this.menuSlotRemove.Text = "Remove";
            this.menuSlotRemove.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.menuSlotRemove.Click += new System.EventHandler(this.MenuSlotRemove_Click);
            // 
            // menuSlotClearAll
            // 
            this.menuSlotClearAll = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSlotClearAll.Name = "menuSlotClearAll";
            this.menuSlotClearAll.Text = "Clear All";
            this.menuSlotClearAll.Click += new System.EventHandler(this.BtnClearAll_Click);
            // 
            // contextMenuSlot
            // 
            this.contextMenuSlot = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuSlot.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuSlotPin,
            this.menuSlotExtractText,
            this.menuSlotRemove,
            this.menuSlotClearAll});
            this.contextMenuSlot.Name = "contextMenuSlot";
            this.contextMenuSlot.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuSlot_Opening);
            // 
            // listViewSlots
            // 
            this.listViewSlots.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSlot,
            this.colType,
            this.colPreview});
            this.listViewSlots.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewSlots.FullRowSelect = true;
            this.listViewSlots.GridLines = true;
            this.listViewSlots.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listViewSlots.Location = new System.Drawing.Point(0, 0);
            this.listViewSlots.MultiSelect = true;
            this.listViewSlots.Name = "listViewSlots";
            this.listViewSlots.Size = new System.Drawing.Size(620, 430);
            this.listViewSlots.SmallImageList = this.imageListThumbs;
            this.listViewSlots.TabIndex = 0;
            this.listViewSlots.UseCompatibleStateImageBehavior = false;
            this.listViewSlots.View = System.Windows.Forms.View.Details;
            this.listViewSlots.ContextMenuStrip = this.contextMenuSlot;
            // 
            // btnClearAll
            // 
            this.btnClearAll.AutoSize = true;
            this.btnClearAll.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
            this.btnClearAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnClearAll.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnClearAll.Name = "btnClearAll";
            this.btnClearAll.TabIndex = 1;
            this.btnClearAll.Text = "Clear All";
            this.btnClearAll.UseVisualStyleBackColor = true;
            this.btnClearAll.Click += new System.EventHandler(this.BtnClearAll_Click);
            // 
            // btnRemoveSelected
            // 
            this.btnRemoveSelected.AutoSize = true;
            this.btnRemoveSelected.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
            this.btnRemoveSelected.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnRemoveSelected.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnRemoveSelected.Name = "btnRemoveSelected";
            this.btnRemoveSelected.TabIndex = 2;
            this.btnRemoveSelected.Text = "Remove Selected";
            this.btnRemoveSelected.UseVisualStyleBackColor = true;
            this.btnRemoveSelected.Click += new System.EventHandler(this.BtnRemoveSelected_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.AutoSize = true;
            this.btnSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnSettings.Margin = new System.Windows.Forms.Padding(0);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.TabIndex = 5;
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.BtnSettings_Click);
            // 
            // flowButtons
            // 
            this.flowButtons.AutoSize = true;
            this.flowButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowButtons.Controls.Add(this.btnClearAll);
            this.flowButtons.Controls.Add(this.btnRemoveSelected);
            this.flowButtons.Controls.Add(this.btnSettings);
            this.flowButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowButtons.Margin = new System.Windows.Forms.Padding(0);
            this.flowButtons.Name = "flowButtons";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Margin = new System.Windows.Forms.Padding(0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Monitoring... (0/10 slots used)";
            // 
            // panelBottom
            // 
            this.panelBottom.AutoSize = true;
            this.panelBottom.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panelBottom.ColumnCount = 1;
            this.panelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelBottom.RowCount = 2;
            this.panelBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.panelBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.panelBottom.Controls.Add(this.flowButtons, 0, 0);
            this.panelBottom.Controls.Add(this.lblStatus, 0, 1);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.panelBottom.TabIndex = 4;
            // 
            // menuFullScreen
            // 
            this.menuFullScreen.Name = "menuFullScreen";
            this.menuFullScreen.Size = new System.Drawing.Size(180, 22);
            this.menuFullScreen.Text = "Full Screen";
            this.menuFullScreen.Click += new System.EventHandler(this.MenuFullScreen_Click);
            // 
            // menuActiveWindow
            // 
            this.menuActiveWindow.Name = "menuActiveWindow";
            this.menuActiveWindow.Size = new System.Drawing.Size(180, 22);
            this.menuActiveWindow.Text = "Active Window";
            this.menuActiveWindow.Click += new System.EventHandler(this.MenuActiveWindow_Click);
            // 
            // menuSelectRegion
            // 
            this.menuSelectRegion.Name = "menuSelectRegion";
            this.menuSelectRegion.Size = new System.Drawing.Size(180, 22);
            this.menuSelectRegion.Text = "Select Region";
            this.menuSelectRegion.Click += new System.EventHandler(this.MenuSelectRegion_Click);
            // 
            // screenshotMenu
            // 
            this.screenshotMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFullScreen,
            this.menuActiveWindow,
            this.menuSelectRegion});
            this.screenshotMenu.Name = "screenshotMenu";
            this.screenshotMenu.Size = new System.Drawing.Size(181, 70);
            // 
            // menuShow
            // 
            this.menuShow.Name = "menuShow";
            this.menuShow.Size = new System.Drawing.Size(180, 22);
            this.menuShow.Text = "Show";
            this.menuShow.Click += new System.EventHandler(this.MenuShow_Click);
            // 
            // menuTrayFullScreen
            // 
            this.menuTrayFullScreen.Name = "menuTrayFullScreen";
            this.menuTrayFullScreen.Size = new System.Drawing.Size(180, 22);
            this.menuTrayFullScreen.Text = "Full Screen";
            this.menuTrayFullScreen.Click += new System.EventHandler(this.MenuFullScreen_Click);
            // 
            // menuTrayActiveWindow
            // 
            this.menuTrayActiveWindow.Name = "menuTrayActiveWindow";
            this.menuTrayActiveWindow.Size = new System.Drawing.Size(180, 22);
            this.menuTrayActiveWindow.Text = "Active Window";
            this.menuTrayActiveWindow.Click += new System.EventHandler(this.MenuActiveWindow_Click);
            // 
            // menuTraySelectRegion
            // 
            this.menuTraySelectRegion.Name = "menuTraySelectRegion";
            this.menuTraySelectRegion.Size = new System.Drawing.Size(180, 22);
            this.menuTraySelectRegion.Text = "Select Region";
            this.menuTraySelectRegion.Click += new System.EventHandler(this.MenuSelectRegion_Click);
            // 
            // menuScreenshot
            // 
            this.menuScreenshot.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuTrayFullScreen,
            this.menuTrayActiveWindow,
            this.menuTraySelectRegion});
            this.menuScreenshot.Name = "menuScreenshot";
            this.menuScreenshot.Size = new System.Drawing.Size(180, 22);
            this.menuScreenshot.Text = "Screenshot";
            // 
            // menuSettings
            // 
            this.menuSettings.Name = "menuSettings";
            this.menuSettings.Size = new System.Drawing.Size(180, 22);
            this.menuSettings.Text = "Settings";
            this.menuSettings.Click += new System.EventHandler(this.BtnSettings_Click);
            // 
            // menuCheckForUpdates
            // 
            this.menuCheckForUpdates.Name = "menuCheckForUpdates";
            this.menuCheckForUpdates.Size = new System.Drawing.Size(180, 22);
            this.menuCheckForUpdates.Text = "Check for Updates";
            this.menuCheckForUpdates.Click += new System.EventHandler(this.MenuCheckForUpdates_Click);
            // 
            // menuToggleWidget
            // 
            this.menuToggleWidget.Name = "menuToggleWidget";
            this.menuToggleWidget.Size = new System.Drawing.Size(180, 22);
            this.menuToggleWidget.Text = "Toggle Widget";
            this.menuToggleWidget.Click += new System.EventHandler(this.MenuToggleWidget_Click);
            // 
            // menuClearAll
            // 
            this.menuClearAll.Name = "menuClearAll";
            this.menuClearAll.Size = new System.Drawing.Size(180, 22);
            this.menuClearAll.Text = "Clear All";
            this.menuClearAll.Click += new System.EventHandler(this.BtnClearAll_Click);
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(180, 22);
            this.menuExit.Text = "Exit";
            this.menuExit.Click += new System.EventHandler(this.MenuExit_Click);
            // 
            // contextMenuTray
            // 
            this.contextMenuTray.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuShow,
            this.menuToggleWidget,
            this.menuScreenshot,
            this.menuSettings,
            this.menuCheckForUpdates,
            this.toolStripSeparator1,
            this.menuClearAll,
            this.toolStripSeparator2,
            this.menuExit});
            this.contextMenuTray.Name = "contextMenuTray";
            this.contextMenuTray.Size = new System.Drawing.Size(181, 120);
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuTray;
            this.notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            this.notifyIcon.Text = "CtrlCV - Clipboard Manager";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.NotifyIcon_DoubleClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(620, 500);
            this.Controls.Add(this.listViewSlots);
            this.Controls.Add(this.panelBottom);
            this.MinimumSize = new System.Drawing.Size(450, 350);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CtrlCV - Clipboard Manager";
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.screenshotMenu.ResumeLayout(false);
            this.contextMenuTray.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
