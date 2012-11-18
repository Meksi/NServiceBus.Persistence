using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Saga;

namespace NServiceBus.Persistense.Tests
{
    public class TestSagaData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public Guid CorrelationId { get; set; }

        public int IntProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public string StringProperty { get; set; }
        public bool BoolProperty { get; set; }
        public List<int> ListIntProperty { get; set; }
    }
}
