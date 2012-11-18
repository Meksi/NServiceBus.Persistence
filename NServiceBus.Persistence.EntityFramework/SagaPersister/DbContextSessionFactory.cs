using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Persistence.Raven;

namespace NServiceBus.Persistence.EntityFramework
{
    public class DbContextSessionFactory : IDisposable
    {
        [ThreadStatic] 
        private static SagaContext _context;

        public SagaContext Context
        {
            get { return _context ?? (_context = new SagaContext()); }
        }

        public void Dispose()
        {
            if (_context == null)
                return;

            _context.Dispose();
            _context = null;
        }

        public void SaveChanges()
        {
            if (_context == null)
                return;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException exc)
            {
                throw new ConcurrencyException("A saga with the same Unique property already existed in the storage. See the inner exception for further details", exc);
            }
            catch (DbUpdateException exc)
            {
                //TODO
                throw;
            }
        }
    }
}
