﻿/* Copyright (c) Citrix Systems Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using XenAdmin.Actions;
using XenAdmin.Controls;
using XenAdmin.CustomFields;
using XenAdmin.Dialogs;
using XenAdmin.Model;
using XenAPI;
using System.Diagnostics;
using XenAdmin.Properties;
using XenAdmin.Core;
using XenAdmin.Commands;


namespace XenAdmin.TabPages
{
    public sealed partial class SnapshotsPage : BaseTabPage
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private VM m_VM = null;
        private bool m_NeedToUpdate = false;
        private bool m_TreeViewEnabled = false;
        private float defaultWidthProperties;

        private readonly ToolTip toolTipDescriptionLabel = new ToolTip();
        private readonly ToolTip toolTipFolderLabel = new ToolTip();
        private readonly ToolTip toolTipTagsLabel = new ToolTip();

        public SnapshotsPage()
        {
            InitializeComponent();
            viewToolStripMenuItem.DropDown = contextMenuStripView;
            this.DoubleBuffered = true;
            base.Text = Messages.SNAPSHOTS_PAGE_TITLE;
            dataGridView.TabIndex = TreeView.TabIndex;
            DataGridView.Sorted += DataGridView_Sorted;
            ConnectionsManager.History.CollectionChanged += History_CollectionChanged;
        }


        void DataGridView_Sorted(object sender, EventArgs e)
        {
            switch (DataGridView.SortedColumn.Index)
            {
                case 0:
                    sortByTypeToolStripMenuItem.Checked = true;
                    sortByNameToolStripMenuItem.Checked = false;
                    sortByCreatedOnToolStripMenuItem.Checked = false;
                    sortBySizeToolStripMenuItem.Checked = false;
                    break;
                case 1:
                    sortByTypeToolStripMenuItem.Checked = false;
                    sortByNameToolStripMenuItem.Checked = true;
                    sortByCreatedOnToolStripMenuItem.Checked = false;
                    sortBySizeToolStripMenuItem.Checked = false;
                    break;
                case 2:
                    sortByTypeToolStripMenuItem.Checked = false;
                    sortByNameToolStripMenuItem.Checked = false;
                    sortByCreatedOnToolStripMenuItem.Checked = true;
                    sortBySizeToolStripMenuItem.Checked = false;
                    break;
                case 3:
                    sortByTypeToolStripMenuItem.Checked = false;
                    sortByNameToolStripMenuItem.Checked = false;
                    sortByCreatedOnToolStripMenuItem.Checked = false;
                    sortBySizeToolStripMenuItem.Checked = true;
                    break;
            }
        }


        public SnapshotTreeView TreeView
        {
            get { return this.snapshotTreeView; }
        }

        public DataGridView DataGridView
        {
            get { return this.dataGridView; }
        }

        public VM VM
        {
            set
            {
                if (m_VM != null)
                {
                    m_VM.Connection.Cache.DeregisterBatchCollectionChanged<VM>(VM_BatchCollectionChanged);
                    m_VM.PropertyChanged -= snapshot_PropertyChanged;
                    foreach (VM snapshot in m_VM.Connection.ResolveAll<VM>(m_VM.snapshots))
                    {
                        snapshot.PropertyChanged -= snapshot_PropertyChanged;
                    }
                }
                if (value != null)
                {
                    m_VM = value;
                    Program.Invoke(Program.MainWindow, () => TreeView.ChangeVMToSpinning(false, null));
                    m_VM.Connection.Cache.RegisterBatchCollectionChanged<VM>(
                        VM_BatchCollectionChanged);
                    m_VM.PropertyChanged += snapshot_PropertyChanged;
                    //Version setup
                    m_TreeViewEnabled = Helpers.MidnightRideOrGreater(m_VM.Connection);
                    toolStripMenuItemScheduledSnapshots.Available = toolStripSeparatorView.Available = Helpers.CowleyOrGreater(m_VM.Connection)
                        && Registry.VMPRFeatureEnabled;
                    if (m_TreeViewEnabled && VM.SnapshotView != SnapshotsView.ListView)
                        TreeViewChecked();
                    else
                    {
                        GridViewChecked();
                    }

                    if (m_TreeViewEnabled)
                    {
                        toolStripButtonTreeView.Enabled = true;
                        toolStripButtonTreeView.ToolTipText = "";

                        revertButton.Enabled = true;
                        toolTipContainerRevertButton.SetToolTip("");
                    }
                    else
                    {
                        toolStripButtonTreeView.Enabled = false;
                        toolStripButtonTreeView.ToolTipText =
                            string.Format(Messages.FEATURE_NOT_AVAILABLE_NEED_MR, Messages.SNAPSHOT_TREE_VIEW);

                        revertButton.Enabled = false;
                        toolTipContainerRevertButton.SetToolTip(
                            string.Format(Messages.FEATURE_NOT_AVAILABLE_NEED_MR, Messages.SNAPSHOT_REVERT));
                    }
                    //toolStripButtonListView.Enabled = toolStripButtonTreeView.Enabled = true;
                    //Refresh items
                    BuildList();
                    RefreshDetailsGroupBox(true);
                    RefreshToolStripButtons();
                    RefreshVMProtectionPanel();
                    RefreshArchiveNowOption();
                }
            }
            get
            {
                return m_VM;
            }
        }

        private void RefreshArchiveNowOption()
        {
            if(Helpers.ClearwaterOrGreater(VM.Connection))
            {
                archiveSnapshotNowToolStripMenuItem.Enabled = archiveSnapshotNowToolStripMenuItem.Visible = false;
                archiveToolStripMenuItem.Enabled = archiveToolStripMenuItem.Visible = false;
            }
            else
            {
                archiveSnapshotNowToolStripMenuItem.Enabled = archiveSnapshotNowToolStripMenuItem.Visible = true;
                archiveToolStripMenuItem.Enabled = archiveToolStripMenuItem.Visible = true;
            }
        }

        private void RefreshVMProtectionPanel()
        {
            if (Helpers.CowleyOrGreater(VM.Connection) && Registry.VMPRFeatureEnabled && !Helpers.ClearwaterOrGreater(VM.Connection))
            {
                panelVMPP.Visible = true;
                var vmpp = VM.Connection.Resolve(VM.protection_policy);
                if (vmpp == null || Helpers.FeatureForbidden(VM.Connection, Host.RestrictVMProtection))
                {
                    labelVMPPInfo.Text = Messages.THIS_VM_IS_NOT_PROTECTED;
                    pictureBoxVMPPInfo.Image = Resources._000_BackupMetadata_h32bit_16;

                    linkLabelVMPPInfo.Text = Helpers.FeatureForbidden(VM.Connection, Host.RestrictVMProtection) ? Messages.TELL_ME_MORE : Messages.VIEW_POLICIES;

                }
                else
                {
                    labelVMPPInfo.Text = string.Format(Messages.THIS_VM_IS_PROTECTED, vmpp.Name);
                    pictureBoxVMPPInfo.Image = Resources._000_Tick_h32bit_16;
                    linkLabelVMPPInfo.Text = Messages.VIEW_POLICIES;
                }
            }
            else
                panelVMPP.Visible = false;
        }



        private void UpdateSpinningIcon()
        {
            ActionBase action = ConnectionsManager.History.Find(SpinningPredicate());
            if (action != null)
            {
                string message = action is VMSnapshotCreateAction ? Messages.SNAPSHOTTING : Messages.VM_REVERTING_ELLIPSIS;
                Program.Invoke(Program.MainWindow, () => TreeView.ChangeVMToSpinning(true, message));
            }
        }

        private Predicate<ActionBase> SpinningPredicate()
        {
            return a => a.VM == VM && !a.IsCompleted && (a is VMSnapshotCreateAction || a is VMSnapshotRevertAction);
        }

        private void History_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            //Program.AssertOnEventThread();
            Program.BeginInvoke(Program.MainWindow, () =>
                                                        {
                                                            if (e.Action == CollectionChangeAction.Add &&
                                                                (e.Element is VMSnapshotCreateAction ||
                                                                 e.Element is VMSnapshotRevertAction))
                                                            {
                                                                ActionBase createAction = (ActionBase)e.Element;
                                                                createAction.Completed += CreateActionCompleted;
                                                                string message = createAction is VMSnapshotCreateAction
                                                                                     ? Messages.SNAPSHOTTING
                                                                                     : Messages.VM_REVERTING_ELLIPSIS;
                                                                if (TreeView.SpinningMessage != Messages.SNAPSHOTTING)
                                                                    Program.Invoke(Program.MainWindow,
                                                                                   () =>
                                                                                   TreeView.ChangeVMToSpinning(true,
                                                                                                               message));
                                                            }
                                                        });
        }

        private void CreateActionCompleted(ActionBase senderAction)
        {
            senderAction.Completed -= CreateActionCompleted;
            ActionBase action = ConnectionsManager.History.Find(SpinningPredicate());
            if (action == null)
            {
                Program.Invoke(Program.MainWindow, () => TreeView.ChangeVMToSpinning(false, null));
            }
        }


        private void VM_BatchCollectionChanged(object sender, EventArgs e)
        {

            if (m_NeedToUpdate)
            {
                BuildList();
                RefreshDetailsGroupBox(true);
#if DEBUG
                Debug.WriteLine("Refreshing the list of snapshots...");
#endif
            }

            m_NeedToUpdate = false;
        }

        private void BuildList()
        {
            if (!this.Visible)
                return;
            lock (TreeView)
            {
                if (VM == null || VM.snapshots == null)
                    return;
                var snapshots = VM.Connection.ResolveAll(VM.snapshots);


                if (snapshots.Count == 0)
                {
                    TreeView.Clear();
                    TreeViewChecked();
                    RefreshDetailsGroupBox(true);
                    toolStripButtonTreeView.Enabled = toolStripButtonListView.Enabled = false; ;
                    return;
                }
                toolStripButtonListView.Enabled = true;
                IList<VM> roots = RefreshDataGridView(snapshots);
                if (m_TreeViewEnabled)
                {
                    toolStripButtonTreeView.Enabled = true;
                    RefreshTreeView(roots);
                    SelectPreviousItemVMTreeView();
                }
                else
                {
                    GridViewChecked();
                }
                UpdateSpinningIcon();
            }
        }

        private void SelectPreviousItemVMTreeView()
        {
            if (TreeView.Items.Count > 0)
            {
                TreeView.Select();
                foreach (ListViewItem item in TreeView.Items)
                {
                    if (item.ImageIndex == SnapshotIcon.VMImageIndex)
                    {
                        SnapshotIcon icon = item as SnapshotIcon;
                        if (icon == null) continue;
                        if (icon.Parent.Selectable)
                        {
                            icon.Parent.Selected = true;
                            icon.Parent.Focused = true;
                            break;
                        }
                        else
                        {
                            foreach (SnapshotIcon sibling in icon.Parent.Children)
                            {
                                if (sibling.Selectable)
                                {
                                    sibling.Selected = true;
                                    sibling.Focused = true;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                RefreshDetailsGroupBox(true);
            }
        }

        private void RefreshTreeView(IList<VM> roots)
        {
            if (TreeView.Parent == null) return;

            TreeView.SuspendLayout();
            TreeView.BeginUpdate();
            TreeView.Clear();
            Debug.WriteLine("Start refreshing Tree");
            SnapshotIcon rootIcon = null;
            rootIcon = new SnapshotIcon(VM.Name, Messages.BASE, null, TreeView, SnapshotIcon.Template);
            TreeView.AddSnapshot(rootIcon);

            //Set VM
            VM vmParent = VM.Connection.Resolve<VM>(VM.parent);
            if (vmParent == null || !vmParent.is_a_snapshot)
            {
                SnapshotIcon vmIcon = new SnapshotIcon(Messages.NOW, "", rootIcon, TreeView, SnapshotIcon.VMImageIndex);
                TreeView.AddSnapshot(vmIcon);
            }

            foreach (VM root in roots)
            {
                if (!(root.is_snapshot_from_vmpp && !toolStripMenuItemScheduledSnapshots.Checked))
                {
                    int icon;
                    if (root.is_snapshot_from_vmpp)
                        icon = root.power_state == vm_power_state.Suspended ? SnapshotIcon.ScheduledDiskMemorySnapshot : SnapshotIcon.ScheduledDiskSnapshot;
                    else
                        icon = root.power_state == vm_power_state.Suspended ? SnapshotIcon.DiskAndMemorySnapshot : SnapshotIcon.DiskSnapshot;
                    DateTime time = root.snapshot_time.ToLocalTime() + root.Connection.ServerTimeOffset;
                    SnapshotIcon currentIcon = new SnapshotIcon(root.name_label, HelpersGUI.DateTimeToString(time, Messages.DATEFORMAT_DMY_HMS, true), rootIcon, TreeView, icon);
                    TreeView.AddSnapshot(currentIcon);
                    currentIcon.Tag = root;
                    BuildTree(root, currentIcon);
                }
                else
                    BuildTree(root, rootIcon);
            }

            if (TreeView.Items.Count < 4)
                TreeView.Clear();

            TreeView.EndUpdate();
            TreeView.ResumeLayout();
            TreeView.PerformLayout();


        }

        private void RefreshDataGridView()
        {
            IList<VM> snapshots = VM.Connection.ResolveAll(VM.snapshots);
            if (snapshots.Count == 0)
            {
                TreeView.Clear();
                TreeViewChecked();
                return;
            }
            RefreshDataGridView(snapshots);
        }

        private IList<VM> RefreshDataGridView(IList<VM> snapshots)
        {

            DataGridView.Rows.Clear();
            IList<VM> roots = new List<VM>();
            for (int i = 0; i < snapshots.Count; i++)
            {
                VM snapshot = snapshots[i];
                if (!(snapshot.is_snapshot_from_vmpp && !toolStripMenuItemScheduledSnapshots.Checked))
                {
                    snapshot.PropertyChanged -= snapshot_PropertyChanged;
                    snapshot.PropertyChanged += snapshot_PropertyChanged;
                    //Build DataGridView
                    SnapshotDataGridViewRow row = new SnapshotDataGridViewRow(snapshot);
                    row.Tag = snapshot;
                    DataGridView.Rows.Add(row);
                }
                VM parent = VM.Connection.Resolve<VM>(snapshot.parent);
                if (parent == null)
                    roots.Add(snapshot);
                else if (!snapshots.Contains(parent) && !roots.Contains(parent))
                    roots.Add(snapshot);


            }
            if (DataGridView.SortedColumn == null)
                DataGridView.Sort(DataGridView.Columns[1], ListSortDirection.Ascending);
            return roots;
        }

        private class SnapshotDataGridViewRow : DataGridViewRow
        {
            private DataGridViewTextBoxCell _type = new DataGridViewTextBoxCell();
            private DataGridViewTextBoxCell _name = new DataGridViewTextBoxCell();
            private DataGridViewTextBoxCell _creationTime = new DataGridViewTextBoxCell();
            private DataGridViewTextBoxCell _size = new DataGridViewTextBoxCell();
            private DataGridViewTextBoxCell _tags = new DataGridViewTextBoxCell();
            private DataGridViewTextBoxCell _description = new DataGridViewTextBoxCell();
            private DataGridViewTextBoxCell _sortType = new DataGridViewTextBoxCell();

            public SnapshotDataGridViewRow(VM snapshot)
            {
                //Icon
                this.Cells.Add(_type);
                _type.Value = snapshot.power_state == vm_power_state.Suspended
                                                                 ? Messages.DISKS_AND_MEMORY
                                                                 : Messages.DISKS_ONLY;
                //Name
                Cells.Add(_name);
                _name.Value = snapshot.name_label;
                //Created On
                Cells.Add(_creationTime);
                _creationTime.Value = snapshot.snapshot_time.ToLocalTime() + snapshot.Connection.ServerTimeOffset;
                //Size
                Cells.Add(_size);
                _size.Value = GetStringSnapshotSize(snapshot);
                //Tags
                Cells.Add(_tags);
                _tags.Value = ConcatWithSeparator(Tags.GetTags(snapshot), ", ");
                //Description
                Cells.Add(_description);
                _description.Value = snapshot.Description != ""
                                         ? snapshot.Description
                                         : Messages.NONE;
            }

        }

        void snapshot_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            if (e.PropertyName == "current_operations")
            {
                VM vm = null;
                if (sender is VM)
                    vm = ((VM)sender);
                else
                    vm = (VM)sender;
                if (vm.current_operations.Count == 0 && !vm.is_a_snapshot)
                    m_NeedToUpdate = true;
#if DEBUG

                foreach (KeyValuePair<string, vm_operations> op in vm.current_operations)
                {
                    Debug.WriteLine(vm.name_label + " " + op.Value);
                }
                if (vm.current_operations.Count == 0)
                    Debug.WriteLine("End of operation");
#endif
            }

            switch (e.PropertyName)
            {
                case "name_label":
                    BuildList();
                    RefreshDetailsGroupBox(true);
                    break;
                case "name_description":
                case "tags":
                case "other_config":
                case "Path":
                    RefreshDetailsGroupBox(true);
                    RefreshDataGridView();
                    RefreshToolStripButtons();
                    break;
                case "snapshots":
                    m_NeedToUpdate = true;
                    break;
                case "protection_policy":
                    RefreshVMProtectionPanel();
                    break;
            }
        }



        private static string GetStringSnapshotSize(VM snapshot)
        {
            long size = GetSnapshotSize(snapshot);

            return Util.DiskSizeString(size);
        }

        private static long GetSnapshotSize(VM snapshot)
        {
            long size = 0;

            foreach (VBD vbd in snapshot.Connection.ResolveAll(snapshot.VBDs))
            {
                if (vbd.type == vbd_type.Disk)
                {
                    VDI vdi = snapshot.Connection.Resolve(vbd.VDI);
                    if (vdi == null || vdi.physical_utilisation == -1)
                        continue;

                    size += vdi.physical_utilisation;
                }
            }

            VDI suspendedVdi = snapshot.Connection.Resolve(snapshot.suspend_VDI);
            if (suspendedVdi != null && suspendedVdi.physical_utilisation > 0)
                size += suspendedVdi.physical_utilisation;
            return size;
        }

        private void BuildTree(VM currentNode, SnapshotIcon currentIcon)
        {
            //Set VM
            if (VM.Connection.Resolve<VM>(VM.parent) == currentNode)
            {
                SnapshotIcon vmIcon = new SnapshotIcon(Messages.NOW, "", currentIcon, TreeView, SnapshotIcon.VMImageIndex);
                TreeView.AddSnapshot(vmIcon);
            }

            //Do childs
            foreach (XenRef<VM> child in currentNode.children)
            {

                if (VM.snapshots.Contains(child))
                {
                    VM childSnapshot = VM.Connection.Resolve<VM>(child);
                    if (!(childSnapshot.is_snapshot_from_vmpp && !toolStripMenuItemScheduledSnapshots.Checked))
                    {
                        int icon;
                        if (childSnapshot.is_snapshot_from_vmpp)
                            icon = childSnapshot.power_state == vm_power_state.Suspended ? SnapshotIcon.ScheduledDiskMemorySnapshot : SnapshotIcon.ScheduledDiskSnapshot;
                        else
                            icon = childSnapshot.power_state == vm_power_state.Suspended ? SnapshotIcon.DiskAndMemorySnapshot : SnapshotIcon.DiskSnapshot;
                        DateTime time = childSnapshot.snapshot_time.ToLocalTime() + childSnapshot.Connection.ServerTimeOffset;
                        SnapshotIcon childIcon = new SnapshotIcon(childSnapshot.name_label, HelpersGUI.DateTimeToString(time, Messages.DATEFORMAT_DMY_HMS, true), currentIcon, TreeView, icon);
                        TreeView.AddSnapshot(childIcon);
                        childIcon.Tag = childSnapshot;
                        BuildTree(childSnapshot, childIcon);
                    }
                    else
                        BuildTree(childSnapshot, currentIcon);
                }
            }
        }

        private void view_SelectionChanged(object sender, System.EventArgs e)
        {
            RefreshDetailsGroupBox(false);
            RefreshToolStripButtons();
            TreeView.Invalidate();
        }

        private void RefreshToolStripButtons()
        {
            IList<VM> snapshots = GetSelectedSnapshots();
            if (snapshots != null && snapshots.Count == 1)
                EnableAllButtons();
            else if (snapshots != null && snapshots.Count > 1)
                EnableDelete();
            else
                DisableAllButtons();


            //Archive now
            archiveSnapshotNowToolStripMenuItem.Available = CanArchive;

        }

        private void DisableAllButtons()
        {
            revertButton.Enabled = false;
            toolTipContainerRevertButton.SetToolTip("");
            saveButton.Enabled = false;
            deleteButton.Enabled = false;
        }

        private void EnableAllButtons()
        {
            if (m_TreeViewEnabled)
            {
                revertButton.Enabled = true;
            }
            else
            {
                revertButton.Enabled = false;
                toolTipContainerRevertButton.SetToolTip(string.Format(
                    Messages.FEATURE_NOT_AVAILABLE_NEED_MR, Messages.SNAPSHOT_REVERT));
            }

            saveButton.Enabled = true;
            deleteButton.Enabled = true;
        }

        private void EnableDelete()
        {
            revertButton.Enabled = false;
            toolTipContainerRevertButton.SetToolTip("");
            saveButton.Enabled = false;
            deleteButton.Enabled = true;
        }

        private void RefreshDetailsGroupBox(bool force)
        {
            IList<VM> selectedSnaps = GetSelectedSnapshots();

            if (selectedSnaps == null || selectedSnaps.Count == 0)
            {
                ShowDisabledPanel();
            }
            else if (toolStripButtonListView.Checked && selectedSnaps.Count == 1)
            {
                ShowDetailsPanel(selectedSnaps[0], force);
            }
            else if (toolStripButtonTreeView.Checked && selectedSnaps.Count == 1)
            {
                ShowDetailsPanel(selectedSnaps[0], force);
            }
            else if (selectedSnaps.Count > 1)
            {
                ShowDetailsPanelMultipleSelection(selectedSnaps);
            }
            else
            {
                ShowDisabledPanel();
            }
        }

        private void ShowDisabledPanel()
        {
            propertiesGroupBox.Enabled = false;
            propertiesGroupBox.Tag = null;
            propertiesTableLayoutPanel.Controls.Remove(tableLayoutPanelMultipleSelection);
            propertiesTableLayoutPanel.Controls.Add(tableLayoutPanelSimpleSelection, 0, 2);
            propertiesGroupBox.Text = String.Format(Messages.SNAPSHOT_CREATED_ON, "");
            nameLabel.Text = "";
            sizeLabel.Text = "";
            labelMode.Text = "";
            descriptionLabel.Text = "";
            tagsLabel.Text = "";
            folderLabel.Text = "";
            screenshotPictureBox.Image = GetNoScreenshotImage();
            tableLayoutPanelSimpleSelection.RowStyles[5].Height = 0;
            tableLayoutPanelSimpleSelection.RowStyles[6].Height = 0;
        }



        private void ShowDetailsPanel(VM snapshot, bool force)
        {
            propertiesGroupBox.Enabled = true;
            if (propertiesGroupBox.Tag != snapshot || force || !propertiesGroupBox.Enabled)
            {
                propertiesTableLayoutPanel.SuspendLayout();
                tableLayoutPanelSimpleSelection.SuspendLayout();
                propertiesTableLayoutPanel.Controls.Remove(tableLayoutPanelMultipleSelection);
                List<CustomField> customFields = CustomFieldsManager.CustomFieldValues(snapshot);
                if (customFields.Count > 0)
                {
                    tableLayoutPanelSimpleSelection.RowStyles[5].Height = 30;
                    customFieldTitle1.Text = String.Format("{0}:", customFields[0].Definition.Name.ToString());
                    customFieldContent1.Text = customFields[0].Value.ToString();
                    tableLayoutPanelSimpleSelection.RowStyles[6].Height = 0;

                    if (customFields.Count == 2)
                    {
                        tableLayoutPanelSimpleSelection.RowStyles[6].Height = 30;
                        customFieldTitle2.Text = customFields[1].Definition.Name.ToString();
                        customFieldContent2.Text = customFields[1].Value.ToString();
                    }

                }
                else
                {
                    tableLayoutPanelSimpleSelection.RowStyles[5].Height = 0;
                    tableLayoutPanelSimpleSelection.RowStyles[6].Height = 0;
                }
                propertiesTableLayoutPanel.Controls.Add(tableLayoutPanelSimpleSelection, 0, 2);
                tableLayoutPanelSimpleSelection.ResumeLayout();
                propertiesTableLayoutPanel.ResumeLayout();
                DateTime time = snapshot.snapshot_time.ToLocalTime() + snapshot.Connection.ServerTimeOffset;
                propertiesGroupBox.Text = String.Format(Messages.SNAPSHOT_CREATED_ON,
                                                        HelpersGUI.DateTimeToString(time, Messages.DATEFORMAT_DMY_HMS, true));
                nameLabel.Text = snapshot.name_label;
                sizeLabel.Text = GetStringSnapshotSize(snapshot);
                labelMode.Text = snapshot.power_state == vm_power_state.Halted ? Messages.DISKS_ONLY : Messages.DISKS_AND_MEMORY;
                descriptionLabel.Text = (snapshot.Description != ""
                                             ? snapshot.Description
                                             : Messages.NONE);
                toolTipDescriptionLabel.SetToolTip(descriptionLabel, 
                                            descriptionLabel.Height == descriptionLabel.MaximumSize.Height ? descriptionLabel.Text : "");

                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorkerDoWork);
                backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
                backgroundWorker.RunWorkerAsync(snapshot);
                tagsLabel.Text = ConcatWithSeparator(Tags.GetTags(snapshot), ", ");
                toolTipTagsLabel.SetToolTip(tagsLabel,
                                            tagsLabel.Height == tagsLabel.MaximumSize.Height ? tagsLabel.Text : "");
                Folder folder = Folders.GetFolder(snapshot);
                if (folder != null)
                    folderLabel.Text = folder.name_label;
                else
                    folderLabel.Text = Messages.NONE;
                toolTipFolderLabel.SetToolTip(folderLabel, 
                                            folderLabel.Height == folderLabel.MaximumSize.Height ? folderLabel.Text : "");
                propertiesGroupBox.Tag = snapshot;
                propertiesButton.Enabled = true;
            }
        }

        private void ShowDetailsPanelMultipleSelection(IList<VM> snapshots)
        {
            propertiesTableLayoutPanel.Controls.Remove(tableLayoutPanelSimpleSelection);
            propertiesTableLayoutPanel.Controls.Add(tableLayoutPanelMultipleSelection, 0, 2);
            nameLabel.Text = String.Format(Messages.SNAPSHOTS_SELECTED, snapshots.Count);
            long totalSize = 0;
            List<string> tags = new List<string>();
            DateTime bigger = DateTime.MinValue;
            DateTime smaller = DateTime.MaxValue;
            foreach (VM snapshot in snapshots)
            {
                totalSize += GetSnapshotSize(snapshot);
                foreach (string tag in Tags.GetTags(snapshot))
                {
                    if (!tags.Contains(tag))
                        tags.Add(tag);
                }
                DateTime time = snapshot.snapshot_time.ToLocalTime() + snapshot.Connection.ServerTimeOffset;
                if (smaller > time)
                    smaller = time;
                if (bigger < time)
                    bigger = time;
            }
            propertiesGroupBox.Text = String.Format("{0} - {1}",
                                                    HelpersGUI.DateTimeToString(smaller, Messages.DATEFORMAT_DMY_HMS, true),
                                                    HelpersGUI.DateTimeToString(bigger, Messages.DATEFORMAT_DMY_HMS, true));
            multipleSelectionTotalSize.Text = Util.DiskSizeString(totalSize);
            multipleSelectionTags.Text = ConcatWithSeparator(tags.ToArray(), ", ");
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorkerDoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            backgroundWorker.RunWorkerAsync(snapshots[0]);
            propertiesButton.Enabled = false;
            propertiesGroupBox.Tag = null;
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((TreeViewButtonsChecked && TreeView.SelectedIndices.Count >= 1) || (GridViewButtonsChecked && DataGridView.SelectedRows.Count >= 1))
                screenshotPictureBox.Image = (Image)e.Result;
            else
            {
                screenshotPictureBox.Image = GetNoScreenshotImage();
            }
        }


        void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                VM snapshot = (VM)e.Argument;
                Image image = GetSnapshotImage(snapshot, false);
                if (image == null)
                {
                    image = GetNoScreenshotImage();
                }
                e.Result = image;
            }
            catch (Exception)
            {
                log.Debug("Error creating the screenshot for simple selection");
            }


        }

        private Image GetNoScreenshotImage()
        {
            Image image;
            image = new Bitmap(100, 75);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 100, 75));
                g.DrawString(Messages.NO_SCREENSHOT, Font, Brushes.White, 10, 30);
            }
            return image;
        }

        private static string ConcatWithSeparator(string[] strings, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string str in strings)
            {
                sb.Append(str);
                sb.Append(separator);
            }

            if (strings.Length > 0)
                sb.Remove(sb.Length - separator.Length, separator.Length);
            else
                sb.Append(Messages.NONE);

            return sb.ToString();
        }



        #region ScreenShot methods
        private Image GetSnapshotImage(VM snapshot, bool style)
        {

            Image image = GetBlobImage(snapshot);

            if (image == null)
                image = GetOtherConfigImage(snapshot);

            if (style)
                return ChangeStyle(image);
            else
                return image;
        }

        private Image ChangeStyle(Image image)
        {
            int SHADOW_OFFSET = 3;
            try
            {
                Image finalImage = new Bitmap(image.Width + SHADOW_OFFSET,
                                              image.Height + SHADOW_OFFSET);
                Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);

                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    //// Draw drop shadow
                    Rectangle shadow = new Rectangle(rectangle.Location, rectangle.Size);
                    shadow.Offset(SHADOW_OFFSET, SHADOW_OFFSET);

                    using (GraphicsPath path = GetRoundedPath(shadow, 4f))
                    using (PathGradientBrush brush = new PathGradientBrush(path))
                    {
                        brush.FocusScales = new PointF(0.8f, 0.8f);
                        brush.SurroundColors = new Color[] { Color.FromArgb(0, Color.Black) };
                        brush.CenterColor = Color.Black;

                        g.FillPath(brush, path);
                    }

                    // Draw image with rounded corners
                    g.SetClip(GetRoundedPath(rectangle, 4f));
                    g.DrawImageUnscaled(image, 0, 0, image.Width, image.Height);

                    //// Darken image
                    using (Brush brush = new LinearGradientBrush(rectangle,
                                                                 Color.FromArgb(100, Color.Black),
                                                                 Color.FromArgb(140, Color.Black),
                                                                 LinearGradientMode.Vertical))
                        g.FillRectangle(brush, rectangle);

                    // Draw shimmer
                    using (GraphicsPath path = GetShimmer(rectangle))
                    using (Brush brush = new LinearGradientBrush(rectangle,
                                                                 Color.FromArgb(100, Color.White),
                                                                 Color.FromArgb(0, Color.White),
                                                                 LinearGradientMode.Vertical))
                        g.FillPath(brush, path);

                    return finalImage;
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        private static GraphicsPath GetShimmer(Rectangle bounds)
        {
            GraphicsPath path = new GraphicsPath();

            int sweepHeight = bounds.Height / 3;
            int rightHeight = bounds.X + sweepHeight;
            int leftHeight = bounds.X + sweepHeight * 2;
            int border = 3;

            path.StartFigure();

            path.AddArc(bounds.Left + border, bounds.Top + border,
                2 * border, 2 * border, 180, 90);
            path.AddLine(bounds.Left + 2 * border, bounds.Top + border,
                bounds.Right - 2 * border, bounds.Top + border);
            path.AddArc(bounds.Right - (3 * border) - 1, bounds.Top + border,
                2 * border, 2 * border, 270, 90);

            path.AddLine(bounds.Right - border - 1, rightHeight,
                bounds.Left + border, leftHeight);

            path.CloseFigure();

            return path;
        }

        private static Image GetOtherConfigImage(VM snapshot)
        {
            try
            {
                string imageStr;
                if (!snapshot.other_config.TryGetValue(VMSnapshotCreateAction.VNC_SNAPSHOT, out imageStr))
                    return null;

                byte[] imageBuffer = Convert.FromBase64String(imageStr);
                using (MemoryStream imageStream = new MemoryStream(imageBuffer))
                    return Image.FromStream(imageStream);
            }
            catch
            {
                return null;
            }
        }

        private static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            float diameter = 2 * radius;

            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter - 1, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter - 1, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private Image GetBlobImage(VM snapshot)
        {
            try
            {
                XenRef<Blob> blobRef;
                if (!snapshot.blobs.TryGetValue(VMSnapshotCreateAction.VNC_SNAPSHOT, out blobRef))
                    return null;

                Blob blob = VM.Connection.Resolve(blobRef);
                if (blob == null)
                    return null;

                using (Stream imageStream = blob.Load())
                    return Image.FromStream(imageStream);
            }
            catch
            {
                return null;
            }
        }
        #endregion

        private void treeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!TreeViewButtonsChecked)
            {
                TreeViewChecked();
                BuildList();
            }
        }

        private bool GridViewButtonsChecked
        {
            get
            {
                return toolStripButtonListView.Checked;
            }
            set
            {
                toolStripButtonListView.Checked = value;
            }
        }

        private bool TreeViewButtonsChecked
        {
            get
            {
                return toolStripButtonTreeView.Checked;
            }
            set
            {
                toolStripButtonTreeView.Checked = value;
            }
        }

        private void TreeViewChecked()
        {
            viewPanel.Controls.Clear();
            viewPanel.Controls.Add(TreeView);
            TreeView.Dock = DockStyle.Fill;
            TreeViewButtonsChecked = true;
            VM.SnapshotView = SnapshotsView.TreeView;
            GridViewButtonsChecked = false;
            TreeView.SelectedItems.Clear();
            foreach (DataGridViewRow row in DataGridView.SelectedRows)
            {
                foreach (ListViewItem item in TreeView.Items)
                {
                    if (row.Tag == item.Tag)
                    {
                        item.Selected = true;
                        break;
                    }
                }
            }
            RefreshToolStripButtons();
        }

        private void GridViewChecked()
        {
            viewPanel.Controls.Clear();
            viewPanel.Controls.Add(DataGridView);
            DataGridView.Dock = DockStyle.Fill;
            GridViewButtonsChecked = true;
            VM.SnapshotView = SnapshotsView.ListView;
            TreeViewButtonsChecked = false;

            RefreshToolStripButtons();
        }

        private void takeSnapshotToolStripButton_Click(object sender, EventArgs e)
        {
            TakeSnapshotCommand command = new TakeSnapshotCommand(Program.MainWindow, VM);
            command.Execute();
        }

        private void deleteToolStripButton_Click(object sender, EventArgs e)
        {
            List<VM> snapshots = GetSelectedSnapshots();
            if (snapshots != null)
                DeleteSnapshots(snapshots);
        }

        private VM GetSelectedSnapshot()
        {
            if (TreeViewButtonsChecked)
            {
                if (TreeView.SelectedItems.Count == 1)
                {
                    return (VM)TreeView.SelectedItems[0].Tag;
                }
            }
            else if (GridViewButtonsChecked)
            {
                if (DataGridView.SelectedRows.Count == 1)
                {
                    return (VM)DataGridView.SelectedRows[0].Tag;
                }
            }
            return null;
        }

        private List<VM> GetSelectedSnapshots()
        {
            if (TreeViewButtonsChecked)
            {
                if (TreeView.SelectedItems.Count > 0)
                {
                    List<VM> snapshots = new List<VM>();
                    foreach (ListViewItem item in TreeView.SelectedItems)
                    {
                        VM snap = (VM)item.Tag;
                        if (snap != null && snap.is_a_snapshot)
                            snapshots.Add(snap);
                    }
                    return snapshots;
                }
            }
            else if (GridViewButtonsChecked)
            {
                if (DataGridView.SelectedRows.Count > 0)
                {
                    List<VM> snapshots = new List<VM>();
                    foreach (DataGridViewRow row in DataGridView.SelectedRows)
                    {
                        VM snap = (VM)row.Tag;
                        if (snap != null && snap.is_a_snapshot)
                            snapshots.Add(snap);
                    }
                    return snapshots;
                }
            }

            return null;
        }


        private void restoreToolStripButton_Click(object sender, EventArgs e)
        {
            VM snapshot = GetSelectedSnapshot();
            if (snapshot != null)
                RevertVM(VM, snapshot);
        }

        private void propertiesButton_Click(object sender, EventArgs e)
        {
            VM snapshot = (VM)propertiesGroupBox.Tag;
            if (snapshot != null)
            {
                PropertiesDialog dialog = new PropertiesDialog(snapshot);
                dialog.ShowDialog();
            }
        }



        private void SetContextMenuMultiSelection()
        {
            contextMenuStrip.Items.Clear();

            if (CanArchive)
                contextMenuStrip.Items.Add(archiveToolStripMenuItem);

            contextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                    viewToolStripMenuItem,
                    sortToolStripSeparator,
                    deleteToolStripMenuItem
                });
        }

        private bool CanArchive
        {
            get
            {
                var vmpp = VM.Connection.Resolve(VM.protection_policy);
                return vmpp != null;
            }
        }

        private void SetContextMenuSnapshot()
        {
            contextMenuStrip.Items.Clear();

            if (m_TreeViewEnabled)
            {
                contextMenuStrip.Items.AddRange(new ToolStripItem[]
                    {
                        revertToolStripMenuItem,
                        saveVMToolStripSeparator
                    });
            }
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                    saveVMToolStripMenuItem,
                    saveTemplateToolStripMenuItem,
                    exportToolStripMenuItem
                });

            if (CanArchive)
                contextMenuStrip.Items.Add(archiveToolStripMenuItem);

            contextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                    separatorDeleteToolStripSeparator,
                    viewToolStripMenuItem
                });

            if (toolStripButtonListView.Checked)
                contextMenuStrip.Items.Add(sortByToolStripMenuItem);

            contextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                    sortToolStripSeparator,
                    deleteToolStripMenuItem,
                    propertiesToolStripMenuItem
                });
        }


        private void SetContextMenuNoneSelected()
        {
            contextMenuStrip.Items.Clear();

            contextMenuStrip.Items.Add(TakeSnapshotToolStripMenuItem);

            if (VM.snapshots.Count > 0)
                contextMenuStrip.Items.Add(viewToolStripMenuItem);
        }


        private void saveAsATemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NewTemplateFromSnapshotCommand(Program.MainWindow, GetSelectedSnapshot()).Execute();
        }

        private void saveAsAVirtualMachineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NewVMFromSnapshotCommand(Program.MainWindow, GetSelectedSnapshot()).Execute();
        }

        private void exportSnapshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ExportSnapshotAsTemplateCommand(Program.MainWindow, GetSelectedSnapshot()).Execute();
        }

        private void snapshotTreeView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo test = TreeView.HitTest(e.Location);
            SnapshotIcon snapIcon = test.Item as SnapshotIcon;
            if (snapIcon != null && snapIcon.Selectable)
                revertButton_Click(sender, e);
        }


        private void sortByToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            switch (item.Name)
            {
                case "sortByTypeToolStripMenuItem":
                    DataGridView.Sort(DataGridView.Columns[0], ListSortDirection.Ascending);
                    DataGridView.Columns[0].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                    break;
                case "sortByNameToolStripMenuItem":
                    DataGridView.Sort(DataGridView.Columns[1], ListSortDirection.Ascending);
                    break;
                case "sortByCreatedOnToolStripMenuItem":
                    DataGridView.Sort(DataGridView.Columns[2], ListSortDirection.Ascending);
                    break;
                //case "sortBySizeToolStripMenuItem":
                //    DataGridView.Sort(DataGridView.Columns[3], ListSortDirection.Ascending);
                //    break;
            }
            if (item.Name != "sortByTypeToolStripMenuItem")
                DataGridView.Columns[0].HeaderCell.SortGlyphDirection = SortOrder.None;
        }



        private void screenshotPictureBox_Click(object sender, EventArgs e)
        {
            VM snapshot = (VM)propertiesGroupBox.Tag;
            if (snapshot != null && screenshotPictureBox.Image.Size.Width > 100)
                new ScreenShotDialog(snapshot.name_label, screenshotPictureBox.Image).ShowDialog();
        }


        private void chevronButton1_Click(object sender, EventArgs e)
        {
            if (contentTableLayoutPanel.ColumnStyles[1].Width == 0)
            {
                contentTableLayoutPanel.ColumnStyles[1].Width = defaultWidthProperties;
                chevronButton1.Text = Messages.HIDE_DETAILS;
                chevronButton1.Image = Resources.PDChevronRight;
            }
            else
            {
                defaultWidthProperties = contentTableLayoutPanel.ColumnStyles[1].Width;
                contentTableLayoutPanel.ColumnStyles[1].Width = 0;
                chevronButton1.Text = Messages.SHOW_DETAILS;
                chevronButton1.Image = Resources.PDChevronLeft;
            }
            TreeView.Invalidate();
        }


        private void revertButton_Click(object sender, EventArgs e)
        {
            VM snapshot = GetSelectedSnapshot();
            if (snapshot != null)
                RevertVM(VM, snapshot);
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            List<VM> snapshots = GetSelectedSnapshots();
            if (snapshots != null)
                DeleteSnapshots(snapshots);
        }


        private void saveButton_Click(object sender, EventArgs e)
        {
            saveMenuStrip.Show(saveButton, 0, saveButton.Height);
        }



        private void saveAsTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NewTemplateFromSnapshotCommand(Program.MainWindow, GetSelectedSnapshot()).Execute();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new NewVMFromSnapshotCommand(Program.MainWindow, GetSelectedSnapshot()).Execute();
        }

        private void exportAsBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ExportSnapshotAsTemplateCommand(Program.MainWindow, GetSelectedSnapshot()).Execute();
        }

        private void gridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GridViewButtonsChecked)
            {
                GridViewChecked();
            }
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (GridViewButtonsChecked)
            {
                if (DataGridView.SelectedRows.Count > 1)
                {
                    SetContextMenuMultiSelection();
                }
                else if (DataGridView.SelectedRows.Count == 1)
                    SetContextMenuSnapshot();
                else
                    SetContextMenuNoneSelected();

            }
            else if (TreeViewButtonsChecked)
            {

                if (TreeView.SelectedItems.Count > 1)
                {
                    SetContextMenuMultiSelection();
                }
                else if (TreeView.SelectedItems.Count < 1)
                {
                    SetContextMenuNoneSelected();
                }
                else if (TreeView.SelectedItems.Count == 1)
                {
                    VM snapshot = (VM)TreeView.SelectedItems[0].Tag;

                    if (snapshot != null && snapshot.is_a_snapshot)
                    {
                        SetContextMenuSnapshot();
                    }
                }
            }

        }

        private void chevronButton1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                chevronButton1_Click(sender, e);
        }

        private static void DeleteSnapshots(List<VM> snapshots)
        {

            DeleteSnapshotCommand deleteSnapshotCommand = new DeleteSnapshotCommand(
                Program.MainWindow, snapshots.ConvertAll<SelectedItem>(input => new SelectedItem(input)));
            deleteSnapshotCommand.Execute();

        }


        private void RevertVM(VM vm, VM snapshot)
        {
            if (vm.current_operations.Count == 0)
            {
                RevertToSnapshotCommand cmd = new RevertToSnapshotCommand(Program.MainWindow, vm, snapshot);
                cmd.Execute();
            }
        }



        private void dataGridView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DataGridView.HitTestInfo hitTestInfo = DataGridView.HitTest(e.X, e.Y);

                if (hitTestInfo.RowIndex >= 0)
                {
                    if (DataGridView.SelectedRows.Count == 1)
                        DataGridView.ClearSelection();
                    DataGridView.Rows[hitTestInfo.RowIndex].Selected = true;

                }
                else
                {
                    DataGridView.ClearSelection();
                }
                contextMenuStrip.Show(DataGridView, e.X, e.Y);

            }
        }

        private void snapshotTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewHitTestInfo hitTestInfo = TreeView.HitTest(e.X, e.Y);
                if (hitTestInfo.Item != null)
                {
                    SnapshotIcon snapshotIcon = hitTestInfo.Item as SnapshotIcon;
                    if (snapshotIcon != null && snapshotIcon.Selectable)
                    {
                        if (TreeView.SelectedItems.Count == 1)
                            TreeView.SelectedItems.Clear();
                        hitTestInfo.Item.Selected = true;
                    }
                    else
                    {
                        TreeView.SelectedItems.Clear();
                    }
                }
                else
                {
                    TreeView.SelectedItems.Clear();
                }
                contextMenuStrip.Show(TreeView, e.X, e.Y);

            }
        }


        private void snapshotTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            SnapshotIcon icon = e.Item as SnapshotIcon;
            if (icon != null && icon.ImageIndex == SnapshotIcon.VMImageIndex)
            {
                TreeView.DoDragDrop(e.Item, DragDropEffects.Move);
            }

        }

        private void snapshotTreeView_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
            Point hitPoint = TreeView.PointToClient(new Point(e.X, e.Y));
            ListViewHitTestInfo info = TreeView.HitTest(hitPoint);
            SnapshotIcon icon = info.Item as SnapshotIcon;
            if (icon != null && (icon.ImageIndex == SnapshotIcon.DiskAndMemorySnapshot || icon.ImageIndex == SnapshotIcon.DiskSnapshot))
            {

                icon.Selected = true;
                if (TreeView.SelectedItems.Count > 1)
                    TreeView.SelectedItems.Clear();
            }

        }

        private void snapshotTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;

        }

        private void snapshotTreeView_DragDrop(object sender, DragEventArgs e)
        {
            Point hitPoint = TreeView.PointToClient(new Point(e.X, e.Y));
            ListViewHitTestInfo info = TreeView.HitTest(hitPoint);
            SnapshotIcon icon = info.Item as SnapshotIcon;
            if (icon != null && (icon.ImageIndex == SnapshotIcon.DiskAndMemorySnapshot || icon.ImageIndex == SnapshotIcon.DiskSnapshot))
            {
                VM snapshot = icon.Tag as VM;
                if (snapshot != null)
                    RevertVM(VM, snapshot);
            }

        }


        private void snapshotTreeView_MouseMove(object sender, MouseEventArgs e)
        {

            SnapshotIcon icon = TreeView.HitTest(e.X, e.Y).Item as SnapshotIcon;
            if (icon != null && icon.ImageIndex == SnapshotIcon.VMImageIndex)
            {
                this.Cursor = Cursors.Hand;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void archiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var vmpp = VM.Connection.Resolve(VM.protection_policy);
            if (vmpp.archive_frequency != vmpp_archive_frequency.never)
            {
                var selectedSnapshots = GetSelectedSnapshots();
                string text = "";
                if (selectedSnapshots.Count == 1)
                    text = string.Format(Messages.ARCHIVE_SNAPSHOT_NOW_TEXT_SINGLE, VM.Connection.Resolve(VM.protection_policy).archive_target_config_location);
                else
                    text = string.Format(Messages.ARCHIVE_SNAPSHOT_NOW_TEXT_MULTIPLE, VM.Connection.Resolve(VM.protection_policy).archive_target_config_location);

                if (new ThreeButtonDialog(
                       new ThreeButtonDialog.Details(SystemIcons.Information, text, Messages.ARCHIVE_VM_PROTECTION_TITLE),
                       ThreeButtonDialog.ButtonYes,
                       ThreeButtonDialog.ButtonNo).ShowDialog() == DialogResult.Yes)
                {
                    foreach (var snapshot in selectedSnapshots)
                    {
                        new ArchiveNowAction(snapshot).RunAsync();
                    }
                }
            }
            else
            {
                if (new ThreeButtonDialog(
                        new ThreeButtonDialog.Details(SystemIcons.Error, Messages.POLICY_DOES_NOT_HAVE_ARCHIVE, Messages.POLICY_DOES_NOT_HAVE_ARCHIVE_TITLE),
                        ThreeButtonDialog.ButtonYes,
                        ThreeButtonDialog.ButtonNo).ShowDialog() == DialogResult.Yes)
                {
                    var dialog = new PropertiesDialog(vmpp);
                    dialog.SelectNewPolicyArchivePage();
                    dialog.ShowDialog(this);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            contextMenuStripView.Show(buttonView, 0, buttonView.Height);
        }

        private void toolStripMenuItemScheduledSnapshots_Click(object sender, EventArgs e)
        {
            toolStripMenuItemScheduledSnapshots.Checked = !toolStripMenuItemScheduledSnapshots.Checked;
            BuildList();
        }

        private void linkLabelVMPPInfo_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Helpers.FeatureForbidden(VM.Connection, Host.RestrictVMProtection))
            {
                VMGroupCommand<VMPP>.ShowUpsellDialog(this);
            }
            else
            {
                var command = new VMGroupCommand<VMPP>(Program.MainWindow, VM);
                command.Execute();
            }
        }

        private void saveMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            saveMenuStrip.Items[saveMenuStrip.Items.Count - 1].Available = CanArchive;
        }

        private void snapshotTreeView_Leave(object sender, EventArgs e)
        {
            TreeView.Invalidate();
        }

        private void snapshotTreeView_Enter(object sender, EventArgs e)
        {
            TreeView.Invalidate();
        }
    }
}
