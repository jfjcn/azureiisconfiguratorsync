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
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class DevelopmentFabricBarrier
    {
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
    }
}
