namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CrossProcessBarrierTest()
        {
            IEnumerable<string> allNames = new List<string>
                                {
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.0_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.1_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.2_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.3_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.4_Web"
                                };

            Func<string, string> escapeMutexName = instanceId => instanceId.Replace("(", ".").Replace(")", ".").Replace(".", "");
            allNames = allNames.Select(escapeMutexName);
            
            var tasks = new List<Task>();
            foreach (var currentName in allNames)
            {
                var peerNames = new List<string>(allNames);
                peerNames.Remove(currentName);

                var c = CrossProcessBarrier.GetInstance(currentName, peerNames, TimeSpan.Zero);
                tasks.Add(Task.Factory.StartNew(c.Wait));
                Trace.TraceInformation("Launched task {0}", currentName);
            }

            Trace.TraceInformation("Waiting for all tasks to reach the barrier");
            Task.WaitAll(tasks.ToArray());
            Trace.TraceInformation("All tasks reached the barrier");
        }
    }
}
