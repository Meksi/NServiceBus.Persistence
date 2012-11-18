using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.Persistence.EntityFramework
{
    public class SagaContext : DbContext
    {
        public DbSet<SagaData> SagaData { get; set; }
    }
}
