using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.Persistence.EntityFramework
{
    public static class ConfigureDbSagaPersister
    {
        public static Configure DbSagaPersister(this Configure config)
        {
            Database.SetInitializer(new SagaDatabaseInitializer());
            config.Configurer.ConfigureComponent<DbContextSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DbUnitOfWork>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<DbSagaPersister>(DependencyLifecycle.InstancePerCall);
            return config;
        }
    }
}
