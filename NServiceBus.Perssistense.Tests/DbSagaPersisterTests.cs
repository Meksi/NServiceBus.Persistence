using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Persistence.EntityFramework;
using NUnit.Framework;


namespace NServiceBus.Persistense.Tests
{
    [TestFixture]
    public class DbSagaPersisterTests
    {
        private DbUnitOfWork uow;
        private DbContextSessionFactory sessionFactory;
        private DbSagaPersister persister;
        private TestSagaData saga;

        [TestFixtureSetUp]
        public void BeforeAllTest()
        {
            Database.SetInitializer(new SagaDatabaseInitializer());
        }

        [SetUp]
        public void BeforeEachTest()
        {
            sessionFactory = new DbContextSessionFactory();
            uow = new DbUnitOfWork(sessionFactory);
            persister = new DbSagaPersister(sessionFactory);
            saga = GetTestSaga();
        }

        [TearDown]
        public void AfterEachTest()
        {
            using (var context = new SagaContext())
            {
                var sagaData = context.SagaData.FirstOrDefault(s => s.Id == saga.Id);
                if (sagaData != null)
                    context.SagaData.Remove(sagaData);
                context.SaveChanges();
            }
        }

        [Test]
        public void CanSaveSaga()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            using (var context = new SagaContext())
            {
                var sagaData = context.SagaData.FirstOrDefault(s => s.Id == saga.Id);
                context.SagaData.Remove(sagaData);
                context.SaveChanges();

                Assert.IsNotNull(sagaData);
                Assert.IsNotNull(sagaData.Data);
                Assert.IsNotNull(sagaData.UniqueProperty);
                Assert.AreEqual(1, sagaData.Version);
            }
        }

        [Test]
        public void CanUpdateSaga()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            uow.Begin();
            persister.Update(saga);
            uow.End();

            using (var context = new SagaContext())
            {
                var sagaData = context.SagaData.FirstOrDefault(s => s.Id == saga.Id);

                Assert.IsNotNull(sagaData);
                Assert.AreEqual(2, sagaData.Version);
            }
        }


        [Test]
        public void CanCompleteSaga()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            uow.Begin();
            persister.Complete(saga);
            uow.End();

            using (var context = new SagaContext())
            {
                var sagaData = context.SagaData.FirstOrDefault(s => s.Id == saga.Id);

                Assert.IsNull(sagaData);
            }
        }

        [Test]
        public void CanGetSagaById()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            uow.Begin();
            var sagaFromDb = persister.Get<TestSagaData>(saga.Id);
            uow.End();

            AssertSagaIsEqual(sagaFromDb);
        }
        
        [Test]
        public void CanGetSagaByGeneralProperty()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            uow.Begin();
            var sagaFromDb = persister.Get<TestSagaData>("StringProperty", saga.StringProperty);
            uow.End();

            AssertSagaIsEqual(sagaFromDb);
        }

        [Test]
        public void CanGetSagaByUniqueProperty()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            uow.Begin();
            var sagaFromDb = persister.Get<TestSagaData>("CorrelationId", saga.CorrelationId);
            uow.End();

            AssertSagaIsEqual(sagaFromDb);
        }

        [Test]
        [ExpectedException(typeof(DbUpdateException))]
        public void SavingSagaWithSameUniquePropertyThrowsException()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            saga.Id = Guid.NewGuid();

            uow.Begin();
            persister.Save(saga);
            uow.End();
        }

        [Test]
        [ExpectedException(typeof (DbUpdateException))]
        public void SavingSagaInDifferentThreadsThrowsException()
        {
            //Worker Thread 1
            uow.Begin();
            persister.Save(saga);
            uow.End();

            //Worker Thread 2
            uow.Begin();
            persister.Save(saga);
            uow.End();
        }

        [Test]
        public void UpdateSagaInDifferentThreadsThrowsConcurrencyException()
        {
            uow.Begin();
            persister.Save(saga);
            uow.End();

            var cts = new CancellationTokenSource();
            var action = new Action(() =>
                                        {
                                            while (true)
                                            {
                                                try
                                                {
                                                    if(cts.Token.IsCancellationRequested)
                                                        break;
                                                    uow.Begin();
                                                    persister.Update(saga);
                                                    uow.End();
                                                }
                                                catch (DbUpdateConcurrencyException exc)
                                                {
                                                    cts.Cancel();
                                                }
                                            }
                                        });

            
            var task1 = new Task(action, cts.Token);
            var task2 = new Task(action, cts.Token);
            task1.Start();
            task2.Start();
            var tasks = new Task[] {task1, task2};
            var index = Task.WaitAny(tasks, 10000);

            Assert.AreNotEqual(-1, index);
            Assert.AreEqual(true, cts.IsCancellationRequested);
        }


        private TestSagaData GetTestSaga()
        {
            return new TestSagaData
                       {
                           Id = Guid.NewGuid(),
                           Originator = "testqueue",
                           OriginalMessageId = Guid.NewGuid().ToString(),

                           CorrelationId = Guid.NewGuid(),

                           BoolProperty = true,
                           DecimalProperty = 99.99m,
                           IntProperty = 1234,
                           StringProperty = "adks dsadas d adkasd asd aldk amd",
                           ListIntProperty = new List<int> {1, 2, 3, 4, 5, 6, 7, 8, 9}
                       };
        }

        private void AssertSagaIsEqual(TestSagaData sagaFromDb)
        {
            Assert.AreEqual(saga.Id, sagaFromDb.Id);
            Assert.AreEqual(saga.BoolProperty, sagaFromDb.BoolProperty);
            Assert.AreEqual(saga.CorrelationId, sagaFromDb.CorrelationId);
            Assert.AreEqual(saga.DecimalProperty, sagaFromDb.DecimalProperty);
            Assert.AreEqual(saga.IntProperty, sagaFromDb.IntProperty);
            Assert.AreEqual(saga.ListIntProperty, sagaFromDb.ListIntProperty);
            Assert.AreEqual(saga.OriginalMessageId, sagaFromDb.OriginalMessageId);
            Assert.AreEqual(saga.Originator, sagaFromDb.Originator);
            Assert.AreEqual(saga.StringProperty, sagaFromDb.StringProperty);
        }
    }
}
