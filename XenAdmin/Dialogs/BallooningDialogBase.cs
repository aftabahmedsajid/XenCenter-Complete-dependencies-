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
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using XenAdmin.Actions;
using XenAdmin.Controls.Ballooning;
using XenAdmin.Core;
using XenAPI;

using XenAdmin.Commands;

namespace XenAdmin.Dialogs
{
    public class BallooningDialogBase : XenDialogBase
    {
        private long origStaticMax;
        private List<VM> vmList;
        protected VMMemoryControlsEdit memoryControls;
        private bool advanced;

        public BallooningDialogBase()
        {
        }

        public BallooningDialogBase(VM vm, bool advanced)
            : base(vm.Connection)
        {
            this.Owner = Program.MainWindow;

            vmList = new List<VM>(1);
            vmList.Add(vm);

            origStaticMax = vm.memory_static_max;

            this.advanced = advanced;
        }

        protected void Initialize()
        {
            // Stuff which has to go after the derived class is constructed
            memoryControls.VMs = vmList;
        }

        protected bool is_a_template
        {
            get { return vmList[0].is_a_template; }
        }

        protected virtual void buttonOK_Click(object sender, EventArgs e)
        {
            memoryControls.UnfocusSpinners();
            if (ConfirmAndChange(this, vmList,
                vmList[0].has_ballooning ? memoryControls.dynamic_min : memoryControls.static_max,  // dynamic_min and _max should stay equal to static_max for VMs without ballooning
                vmList[0].has_ballooning ? memoryControls.dynamic_max : memoryControls.static_max,
                memoryControls.static_max, origStaticMax, advanced))
            {
                Close();
            }
        }

        private static List<ChangeMemorySettingsAction> ConfirmAndCalcActions(IWin32Window parentWindow, List<VM> VMs, long dynamic_min, long dynamic_max, long static_max, long prev_static_max, bool advanced, bool suppressHistory)
        {
            if (prev_static_max / Util.BINARY_MEGA == static_max / Util.BINARY_MEGA)  // don't want to throw warning dialog just for rounding errors
            {
                if (dynamic_min == static_max)
                    dynamic_min = prev_static_max;
                if (dynamic_max == static_max)
                    dynamic_max = prev_static_max;
                static_max = prev_static_max;
            }
            else
            {
                // If not all VMs Halted, confirm static_max changes, and abort if necessary
                foreach (VM vm in VMs)
                {
                    if (vm.power_state != vm_power_state.Halted)
                    {
                        // Six(!) possible messages depending on the exact configuration.
                        // (Could have an additional message for VMs without ballooning but with tools, e.g. on a free host, for which we can do a
                        // clean shutdown: but having a false positive for the scarier message about force shutdown is not too bad).
                        // We assume that all VMs have the same has_ballooning.
                        string msg;
                        if (advanced)
                            msg = (VMs.Count == 1 ? Messages.CONFIRM_CHANGE_STATIC_MAX_SINGULAR : Messages.CONFIRM_CHANGE_STATIC_MAX_PLURAL);
                        else if (vm.has_ballooning && !Helpers.FeatureForbidden(vm, Host.RestrictDMC))
                            msg = (VMs.Count == 1 ? Messages.CONFIRM_CHANGE_MEMORY_MAX_SINGULAR : Messages.CONFIRM_CHANGE_MEMORY_MAX_PLURAL);
                        else
                            msg = (VMs.Count == 1 ? Messages.CONFIRM_CHANGE_MEMORY_SINGULAR : Messages.CONFIRM_CHANGE_MEMORY_PLURAL);
                        if (DialogResult.Yes != new ThreeButtonDialog(
                                new ThreeButtonDialog.Details(SystemIcons.Warning, msg, Messages.XENCENTER),
                                ThreeButtonDialog.ButtonYes,
                                ThreeButtonDialog.ButtonNo).ShowDialog(parentWindow))
                            return null;
                        break;
                    }
                }
            }

            List<ChangeMemorySettingsAction> actions = new List<ChangeMemorySettingsAction>(VMs.Count);
            foreach (VM vm in VMs)
            {
                ChangeMemorySettingsAction action = new ChangeMemorySettingsAction(
                    vm,
                    string.Format(Messages.ACTION_CHANGE_MEMORY_SETTINGS, vm.Name),
                    vm.memory_static_min, dynamic_min, dynamic_max, static_max,VMOperationCommand.WarningDialogHAInvalidConfig,VMOperationCommand.StartDiagnosisForm, suppressHistory);
                actions.Add(action);
            }

            return actions;
        }

        public static bool ConfirmAndChange(IWin32Window parentWindow, List<VM> VMs, long dynamic_min, long dynamic_max, long static_max, long prev_static_max, bool advanced)
        {
            List<ChangeMemorySettingsAction> actions = ConfirmAndCalcActions(parentWindow, VMs, dynamic_min, dynamic_max, static_max, prev_static_max, advanced, false);
            if (actions == null)
                return false;
            foreach (ChangeMemorySettingsAction action in actions)
                action.RunAsync();
            return true;
        }

        public static ChangeMemorySettingsAction ConfirmAndReturnAction(IWin32Window parentWindow, VM vm, long dynamic_min, long dynamic_max, long static_max, long prev_static_max, bool advanced)
        {
            List<VM> vms = new List<VM>(1);
            vms.Add(vm);
            List<ChangeMemorySettingsAction> actions = ConfirmAndCalcActions(parentWindow, vms, dynamic_min, dynamic_max, static_max, prev_static_max, advanced, true);
            if (actions == null || actions.Count == 0)
                return null;
            else
                return actions[0];
        }
    }
}
