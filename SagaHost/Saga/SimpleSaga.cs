using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Saga;
using SagaHost.Messages;

namespace SagaHost
{
    public class SimpleSaga : Saga<SimpleSagaData>,
        IAmStartedByMessages<StartingCommand>,
        IHandleMessages<FinishingCommand>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartingCommand>(saga => saga.OrderId, command => command.OrderId);
            ConfigureMapping<FinishingCommand>(saga => saga.OrderId, command => command.OrderId);
        }
        public void Handle(StartingCommand command)
        {
            Data.OrderId = command.OrderId;
            Data.Count = command.Count;

            Bus.SendLocal(new FinishingCommand {OrderId = Data.OrderId});
        }

        public void Handle(FinishingCommand message)
        {
            Console.WriteLine("Finishing order. Id = {0}, Count = {1}", Data.OrderId, Data.Count);
            MarkAsComplete();
        }
    }
}
