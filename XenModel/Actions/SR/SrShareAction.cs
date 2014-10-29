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
using XenAdmin.Network;
using XenAPI;


namespace XenAdmin.Actions
{
    /// <summary>
    /// Turns an unshared SR into a shared SR.
    /// </summary>
    public class SrShareAction : SrRepairAction
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SrShareAction(IXenConnection connection, SR sr)
            : base(connection, sr,true)
        {
        }

        protected override void Run()
        {
            try
            {
                Description = string.Format(Messages.SR_SHARE_SETTING, SR.Name);

                XenAPI.SR.set_shared(Session, SR.opaque_ref, true);

                base.Run();
            }
            catch
            {
                rollback();
                throw;
            }
        }

        /// <summary>
        /// Destroys any PBDs on the SR and sets the SR to be unshared.
        /// 
        /// No throw.
        /// </summary>
        private void rollback()
        {
            try
            {
                Description = string.Format(Messages.SR_SHARE_REVERTING2, SR.Name);

                foreach (PBD broke in Connection.ResolveAll(SR.PBDs))
                {
                    if (!broke.currently_attached && !broke.Locked)
                        PBD.destroy(Session, broke.opaque_ref);
                }

                XenAPI.SR.set_shared(Session, SR.opaque_ref, false);

                Description = string.Format(Messages.SR_SHARE_REVERTED, SR.Name);
            }
            catch (Exception e)
            {
                log.Error("Exception rolling back SR shared action");
                log.Error(e,e);
            }
        }
    }
}
