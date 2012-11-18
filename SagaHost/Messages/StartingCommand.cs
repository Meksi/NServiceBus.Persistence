using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;

namespace SagaHost.Messages
{
    public class StartingCommand : ICommand
    {
        public Guid OrderId { get; set; }
        public int Count { get; set; }
    }
}
