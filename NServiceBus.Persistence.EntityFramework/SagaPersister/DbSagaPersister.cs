using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NServiceBus.Saga;

namespace NServiceBus.Persistence.EntityFramework
{
    public class DbSagaPersister : ISagaPersister
    {
        private readonly DbContextSessionFactory _sessionFactory;
        private SagaContext Context { get { return _sessionFactory.Context; } }

        public DbSagaPersister(DbContextSessionFactory factory)
        {
            _sessionFactory = factory;
        }

        public void Save(ISagaEntity saga)
        {
            var sagaData = new SagaData
                               {
                                   Id = saga.Id,
                                   Data = GetXmlForSaga(saga),
                                   Version = 1
                               };
            UpdateUniquePropertyForSaga(saga, sagaData);

            Context.SagaData.Add(sagaData);
        }

        public void Update(ISagaEntity saga)
        {
            var sagaData = Context.SagaData.FirstOrDefault(s => s.Id == saga.Id);
            if (sagaData == null)
                return;

            sagaData.Data = GetXmlForSaga(saga);
            sagaData.Version++;
            UpdateUniquePropertyForSaga(saga, sagaData);
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            var sagaData = Context.SagaData.FirstOrDefault(s => s.Id == sagaId);
            return sagaData == null ? default(T) : GetSagaForXml<T>(sagaData.Data);
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            if (IsUniqueProperty<T>(property))
                return GetByUniqueProperty<T>(property, value);

            return GetByXmlQuery<T>(property, value);
        }

        public void Complete(ISagaEntity saga)
        {
            var sagaData = Context.SagaData.FirstOrDefault(s => s.Id == saga.Id);
            if (sagaData == null)
                return;

            Context.SagaData.Remove(sagaData);
        }

        #region Xml

        private string GetXmlForSaga(ISagaEntity saga)
        {
            var serializer = new XmlSerializer(saga.GetType());
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, saga);
            return stringWriter.ToString();
        }

        private T GetSagaForXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            var stringReader = new StringReader(xml);
            var obj = serializer.Deserialize(stringReader);
            return (T) obj;
        }

        #endregion

        #region Unique Property

        private void UpdateUniquePropertyForSaga(ISagaEntity saga, SagaData sagaData)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);
            sagaData.UniqueProperty = uniqueProperty != null ? GetUniqueProperty(saga.GetType(), uniqueProperty.Value) : Guid.NewGuid().ToString();
        }

        private bool IsUniqueProperty<T>(string property)
        {
            //TODO Cache?
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(typeof (T));
            if (uniqueProperty == null)
                return false;
            return uniqueProperty.Name == property;
        }

        private string GetUniqueProperty(Type sagaType, KeyValuePair<string, object> uniqueProperty)
        {
            if (uniqueProperty.Value == null)
                throw new ArgumentNullException("uniqueProperty", string.Format("Property {0} is marked with the [Unique] attribute on {1} but contains a null value. Please make sure that all unique properties are set on your SagaData and/or that you have marked the correct properies as unique.", uniqueProperty.Key, sagaType.Name));

            //use MD5 hash to get a 16-byte hash of the string
            var provider = new MD5CryptoServiceProvider();
            var inputBytes = Encoding.Default.GetBytes(uniqueProperty.Value.ToString());
            var hashBytes = provider.ComputeHash(inputBytes);
            //generate a guid from the hash:
            var value = new Guid(hashBytes);

            var id = string.Format(string.Format("{0}/{1}/{2}", sagaType.FullName, uniqueProperty.Key, value));

            //raven has a size limit of 255 bytes == 127 unicode chars
            if (id.Length > 127)
            {
                //generate a guid from the hash:
                var key = new Guid(provider.ComputeHash(Encoding.Default.GetBytes(sagaType.FullName + uniqueProperty.Key)));

                id = string.Format(string.Format("MoreThan127/{0}/{1}", key, value));
            }
            return id;
        }

        #endregion

        #region Queries

        private T GetByUniqueProperty<T>(string property, object value) where T : ISagaEntity
        {
            using (var context = new SagaContext())
            {
                var uniqueValue = GetUniqueProperty(typeof(T), new KeyValuePair<string, object>(property, value));
                var sagaData = context.SagaData.FirstOrDefault(s => s.UniqueProperty == uniqueValue);
                return sagaData == null ? default(T) : GetSagaForXml<T>(sagaData.Data);
            }
        }

        private T GetByXmlQuery<T>(string property, object value) where T : ISagaEntity
        {
            using (var context = new SagaContext())
            {
                var query = String.Format(@"SELECT * FROM dbo.SagaData WHERE Data.value('(/{0}//{1})[1]', 'nvarchar(max)') = '{2}'",
                        typeof(T).Name, property, value);
                var sagaData = context.SagaData.SqlQuery(query).FirstOrDefault();
                return sagaData == null ? default(T) : GetSagaForXml<T>(sagaData.Data);
            }
        }

        #endregion
    }
}
