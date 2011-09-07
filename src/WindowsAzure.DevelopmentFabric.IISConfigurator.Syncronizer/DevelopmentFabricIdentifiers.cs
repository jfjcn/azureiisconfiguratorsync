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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.ServiceRuntime;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal class DevelopmentFabricIdentifiers
    {
        internal DevelopmentFabricIdentifiers(string value)
        {
            if (!ServerManagerBarrier.InDevFabric)
                throw new NotSupportedException("Must be running in development fabric");

            var match = Regex.Match(value, pattern);
            DeploymentId = int.Parse(match.Groups["deploymentid"].Value);
            Name = match.Groups["name"].Value;
            InstanceId = int.Parse(match.Groups["instanceid"].Value);
        }
        private const string pattern = @"^deployment\((?<deploymentid>\d+)\)\.(?<name>.+)\.(?<instanceid>\d+)(_Web)?$";
        public static bool IsDID(string value)
        {
            return Regex.IsMatch(value, pattern);
        }

        public int DeploymentId { get; set; }
        public string Name { get; set; }
        public int InstanceId { get; set; }

        public override string ToString()
        {
            return string.Format("deployment({0}).{1}.{2}_Web", DeploymentId, Name, InstanceId);
        }
        public bool BelongsToSameDeployment(string otherDeploymentId)
        {
            return BelongsToSameDeployment(new DevelopmentFabricIdentifiers(otherDeploymentId));
        }
        public bool BelongsToSameDeployment(DevelopmentFabricIdentifiers other)
        {
            return this.DeploymentId == other.DeploymentId && this.Name == other.Name;
        }
        public static DevelopmentFabricIdentifiers Current
        {
            get { return new DevelopmentFabricIdentifiers(RoleEnvironment.CurrentRoleInstance.Id + "_Web"); }
        }
        public static List<DevelopmentFabricIdentifiers> AzureRoles
        {
            get { return RoleEnvironment.CurrentRoleInstance.Role.Instances.Select(ri => ri.Id).Select(name => new DevelopmentFabricIdentifiers(name)).ToList(); }
        }
        public static int NumberOfRoles
        {
            get { return DevelopmentFabricIdentifiers.AzureRoles.Where(DevelopmentFabricIdentifiers.Current.BelongsToSameDeployment).Count(); }
        }
        public static List<string> PeerRoles
        {
            get
            {
                // Microsoft.WindowsAzure.ServiceRuntime.CurrentRoleInstanceImp // Microsoft.WindowsAzure.ServiceRuntime.ExternalRoleInstanceImpl

                return RoleEnvironment.CurrentRoleInstance.Role.Instances.Where(x => x.GetType().Name.Equals("ExternalRoleInstanceImpl")).Select(ri => ri.Id).ToList();
            }
        }
    }
}
