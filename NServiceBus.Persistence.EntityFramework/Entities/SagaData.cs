using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.Persistence.EntityFramework
{
    [Table("SagaData")]
    public class SagaData
    {
        [Key]
        public Guid Id { get; set; }
        [Column(TypeName = "xml")]
        public string Data { get; set; }
        [ConcurrencyCheck]
        public long Version { get; set; }
        [MaxLength(256)]
        public string UniqueProperty { get; set; }
    }
}
