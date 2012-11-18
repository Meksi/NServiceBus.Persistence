using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Persistence.EntityFramework;

namespace TestClient
{
    class Program
    {
        private static void Main(string[] args)
        {
            Database.SetInitializer(new SagaDatabaseInitializer());
            var sessionFactory = new DbContextSessionFactory();
            var persister = new DbSagaPersister(sessionFactory);
            var saga = new SimpleSagaData
                           {
                               Id = Guid.NewGuid(),
                               BusinessId = 1
                           };
            persister.Save(saga);

            var action = new Action(() =>
                                        {
                                            while (true)
                                            {
                                                try
                                                {
                                                    persister.Update(saga);
                                                }
                                                catch (DbUpdateConcurrencyException exc)
                                                {
                                                    Console.WriteLine("Optimistic concurrency check failed.");
                                                }
                                            }
                                        });

            var task1 = new Task(action);
            var task2 = new Task(action);
            task1.Start();
            task2.Start();

            Console.WriteLine("Processing...");
            Console.ReadLine();
        }
    }
}
