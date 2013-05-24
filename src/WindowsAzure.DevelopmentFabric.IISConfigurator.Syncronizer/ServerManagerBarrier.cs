// -----------------------------------------------------------------------
// <copyright file="DevelopmentFabricIdentifiers.cs" company="Microsoft">
//
// Copyright 2011, Christian Geuer-Pollmann <geuerp@apache.org>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// -----------------------------------------------------------------------

namespace WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Microsoft.Web.Administration;
    using Microsoft.Win32;
    using Microsoft.WindowsAzure.ServiceRuntime;

    /// <summary>
    /// Utility for developing/testing in the local development fabric. 
    /// 
    /// Sets the Windows identity of the IIS application pools in the local development fabric to a specified user identity. 
    /// </summary>
    /// <see href="http://www.wadewegner.com/2011/01/programmatically-changing-the-apppool-identity-in-a-windows-azure-web-role/#comment-4251"/>
    /// <para>
    /// This is necessary to ensure that code running locally can access production storage, 
    /// when the local network is protected by a firewall such as TMG. 
    /// </para>
    public static class ServerManagerBarrier
    {
        private static string GetRegistryValue(string key)
        {
            var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft EMIC\Cloud\VENUS-C");
            if (regKey != null)
            {
                var regVal = regKey.GetValue(key) as string;
                if (!string.IsNullOrEmpty(regVal))
                    return regVal;
            }

            return null;
        }

        // Windows Registry Editor Version 5.00
        //
        // [HKEY_CURRENT_USER\Software\Microsoft EMIC\Cloud\VENUS-C]
        // "DomainUserName"="EUROPE\\chgeuer"
        // "DomainPassword"="PASSWORD"
        /// <summary>
        /// Sets the Windows identity of the IIS application pools in the local development fabric to a specified user identity. 
        /// </summary>
        /// <see href="http://www.wadewegner.com/2011/01/programmatically-changing-the-apppool-identity-in-a-windows-azure-web-role/#comment-4251"/>
        /// <para>
        /// This is necessary to ensure that code running locally can access production storage, 
        /// when the local network is protected by a firewall such as TMG. 
        /// </para>
        [DebuggerNonUserCode]
        public static void TweakIdentityWhenRunningInCorpnet()
        {
            if (!RoleEnvironment.IsAvailable || !RoleEnvironment.IsEmulated)
            {
                Trace.TraceInformation(
                    "MicrosoftCorpnetAuthenticationFixer: Not running in development fabric, no fix necessary");
                return;
            }

            var appPoolUser = GetRegistryValue("DomainUserName");
            var appPoolPass = GetRegistryValue("DomainPassword");

            if (string.IsNullOrEmpty(appPoolUser) || string.IsNullOrEmpty(appPoolPass))
            {
                Trace.TraceInformation("MicrosoftCorpnetAuthenticationFixer: No credentials to fix");
                return;
            }

            Action<ServerManager> updateIdentity = (serverManager) =>
            {
                var sitename = RoleEnvironment.CurrentRoleInstance.Id + "_Web";
                var appPoolNames = serverManager.Sites[sitename].Applications.Select(app => app.ApplicationPoolName).ToList();

                foreach (var appPoolName in appPoolNames)
                {
                    var pool = serverManager.ApplicationPools[appPoolName];

                    pool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    pool.ProcessModel.UserName = appPoolUser;
                    pool.ProcessModel.Password = appPoolPass;
                }
                serverManager.CommitChanges();
            };

            ApplyServerManagerActions(updateIdentity);

            Trace.TraceInformation(string.Format("Instance {0} updated application pools", RoleEnvironment.CurrentRoleInstance.Id));
        }

        public static void SerializeInDevelopmentFabric(Action action, string mutexName = "SerializeInDevelopmentFabric")
        {
            if (!RoleEnvironment.IsEmulated)
            {
                action();
                return;
            }

            #region Barrier to have all instances wait for their peers to be at the same spot.

            Func<string, string> escapeMutexName = instanceId => instanceId.Replace("(", ".").Replace(")", ".").Replace(".", "");
            var currentMutexName = escapeMutexName(RoleEnvironment.CurrentRoleInstance.Id);
            var peerMutexNames = DevelopmentFabricIdentifiers.PeerRoles.Select(escapeMutexName);
            var cpb = CrossProcessBarrier.GetInstance(currentMutexName, peerMutexNames, TimeSpan.FromSeconds(3));
            cpb.Wait();

            Trace.TraceInformation(string.Format("Barrier passed at {0}", DateTime.UtcNow.ToLongTimeString()));

            #endregion

            #region One development fabric role instance at a time can modify app pool now

            // The global mutex ensures that only one instance at a time attempts to define appPool identities.  
            var mutex = new Mutex(initiallyOwned: false, name: mutexName);
            try
            {
                mutex.WaitOne();

                action();
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            #endregion
        }

        // [DebuggerNonUserCode]        
        public static void ApplyServerManagerActions(Action<ServerManager> serverManagerActions, bool commitChanges = false)
        {
            Action action = () =>
                {
                    // ServerManager in %WinDir%\System32\InetSrv\Microsoft.Web.Administration.dll
                    using (var sm = new ServerManager())
                    {
                        serverManagerActions(sm);

                        if (commitChanges)
                        {
                            sm.CommitChanges();
                        }
                    }
                };

            SerializeInDevelopmentFabric(action, mutexName: typeof (ServerManagerBarrier).FullName);
        }
    }
}