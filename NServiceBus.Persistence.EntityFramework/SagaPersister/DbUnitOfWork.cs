using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Persistence.EntityFramework
{
    public class DbUnitOfWork : IManageUnitsOfWork
    {
        private readonly DbContextSessionFactory sessionFactory;

        public DbUnitOfWork(DbContextSessionFactory factory)
        {
            sessionFactory = factory;
        }

        public void Begin()
        {
            
        }

        public void End(Exception ex = null)
        {
            if (ex == null)
                sessionFactory.SaveChanges();

            sessionFactory.Dispose();
        }
    }
}
