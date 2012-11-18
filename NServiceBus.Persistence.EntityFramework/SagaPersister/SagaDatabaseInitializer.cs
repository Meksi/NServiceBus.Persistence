using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.Persistence.EntityFramework
{
    public class SagaDatabaseInitializer : DropCreateDatabaseIfModelChanges<SagaContext>
    {
        protected override void Seed(SagaContext context)
        {
            context.Database.ExecuteSqlCommand("ALTER TABLE dbo.SagaData ADD CONSTRAINT uc_Unique UNIQUE([UniqueProperty])");
        }
    }
}
