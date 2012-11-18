using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.EntityFramework;
using SagaHost.Messages;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace SagaHost 
{
	
	public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization, IWantToRunAtStartup
    {
        public IBus Bus { get; set; }
	    public void Init()
	    {
	        Configure.With()
	                 .DefaultBuilder()
                     .DbSagaPersister()
                     .IsolationLevel(IsolationLevel.ReadCommitted);
	    }

	    public void Run()
	    {
	        var task = new Task(() =>
	                                {
	                                    while (true)
	                                    {
	                                        Bus.SendLocal(new StartingCommand
	                                                          {
	                                                              OrderId = Guid.NewGuid(),
	                                                              Count = 10
	                                                          });
                                            Thread.Sleep(1);
                                        }
                                        
	                                });
           //task.Start();
	    }

	    public void Stop()
	    {
	        
	    }
    }
}