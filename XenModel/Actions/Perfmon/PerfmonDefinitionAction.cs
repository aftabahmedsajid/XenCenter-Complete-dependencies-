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
using System.Text;
using XenAPI;
using XenAdmin.Alerts;
using XenAdmin.Core;
using XenAdmin.Model;


namespace XenAdmin.Actions
{
    public class PerfmonDefinitionAction : AsyncAction
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IXenObject xo;
        private readonly List<PerfmonDefinition> perfmonDefinitions;

        public PerfmonDefinitionAction(IXenObject xo, List<PerfmonDefinition> perfmonDefinitions, bool suppressHistory)
            : base(xo.Connection, Messages.ACTION_SAVE_ALERTS, string.Format(Messages.ACTION_SAVING_ALERTS_FOR, xo), suppressHistory)
        {
            this.xo = xo;
            this.perfmonDefinitions = perfmonDefinitions;

            string type = xo.GetType().Name.ToLowerInvariant();
            ApiMethodsToRoleCheck.Add(type + ".remove_from_other_config", PerfmonDefinition.PERFMON_KEY_NAME);
            if (perfmonDefinitions != null && perfmonDefinitions.Count != 0)
                ApiMethodsToRoleCheck.Add(type + ".add_to_other_config", PerfmonDefinition.PERFMON_KEY_NAME);
        }

        protected override void Run()
        {
            if (xo == null)
                return;

            if (perfmonDefinitions == null || perfmonDefinitions.Count == 0)
            {
                Helpers.RemoveFromOtherConfig(Session, xo, PerfmonDefinition.PERFMON_KEY_NAME);
            }
            else
            {
                string perfmonConfigXML = PerfmonDefinition.GetPerfmonDefinitionXML(perfmonDefinitions);
                Helpers.SetOtherConfig(Session, xo, PerfmonDefinition.PERFMON_KEY_NAME, perfmonConfigXML);
            }

            var hosts = new List<Host>();

            Host theHost = xo as Host;
            if (theHost == null)
            {
                VM vm = xo as VM;
                if (vm == null)
                {
                    SR sr = xo as SR;
                    if (sr != null)
                    {
                        foreach (var pbdRef in sr.PBDs)
                        {
                            PBD pbd = sr.Connection.Resolve(pbdRef);
                            if (pbd == null)
                                continue;

                            var host = pbd.Connection.Resolve(pbd.host);
                            if (host != null)
                                hosts.Add(host);
                        }
                    }
                }
                else
                {
                    var host = vm.Home();
                    if (host != null)
                        hosts.Add(host);
                }
            }
            else
            {
                hosts.Add(theHost);
            }

            foreach (var host in hosts)
            {
                try
                {
                    //NB The refresh causes the server to re-read the configuration
                    //immediately. But even if the refresh fails, the change will be
                    //noticed, just a bit later.

                    new ExecutePluginAction(host.Connection, host,
                        XenServerPlugins.PLUGIN_PERFMON_PLUGIN,
                        XenServerPlugins.PLUGIN_PERFMON_FUNCTION_REFRESH,
                        new Dictionary<string, string>(), true).RunExternal(Session);
                }
                catch (Exception e)
                {
                    // Handle perfmon randomly being stopped
                    if (e.Message.StartsWith(XenServerPlugins.PLUGIN_PERFMON_ERROR_NOT_RUNNING))
                    {
                        // start perfmon and try again
                        try
                        {
                            new ExecutePluginAction(host.Connection, host,
                                XenServerPlugins.PLUGIN_PERFMON_PLUGIN,
                                XenServerPlugins.PLUGIN_PERFMON_FUNCTION_START,
                                new Dictionary<string, string>(), true).RunExternal(Session);

                            new ExecutePluginAction(host.Connection, host,
                                XenServerPlugins.PLUGIN_PERFMON_PLUGIN,
                                XenServerPlugins.PLUGIN_PERFMON_FUNCTION_REFRESH,
                                new Dictionary<string, string>(), true).RunExternal(Session);
                        }
                        catch (Exception ex)
                        {
                            log.DebugFormat("Perfmon refresh failed ({0}). Alerts will start being produced within half an hour.", ex.Message);
                        }
                    }
                    else
                    {
                        log.DebugFormat("Perfmon refresh failed ({0}). Alerts will start being produced within half an hour.", e.Message);
                    }
                }
            }
        }
    }
}