using log4net.Appender.Extensions;
using log4net.Appender.Language;
using log4net.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;

namespace log4net.Appender
{
    public class AzureTableAppender : BufferingAppenderSkeleton
    {
        private CloudStorageAccount _account;
        private CloudTableClient _client;
        private CloudTable _table;

        public string ConnectionStringName { get; set; }

        private string _connectionString;

        public string ConnectionString
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ConnectionStringName))
                {
                    return Util.GetConnectionString(ConnectionStringName);
                }
                if (string.IsNullOrEmpty(_connectionString))
                    throw new ApplicationException(Resources.AzureConnectionStringNotSpecified);
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }


        private string _tableName;

	    public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_tableName))
                    throw new ApplicationException(Resources.TableNameNotSpecified);
                return _tableName;
            }
            set
            {
                _tableName = value;
            }
        }

        public bool PropAsColumn { get; set; }

	    private PartitionKeyTypeEnum _partitionKeyType = PartitionKeyTypeEnum.LoggerName;
        public PartitionKeyTypeEnum PartitionKeyType
        {
            get { return _partitionKeyType; }
            set { _partitionKeyType = value; }
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            var grouped = events.GroupBy(evt => evt.LoggerName);

            foreach (var group in grouped)
            {
                foreach (var batch in group.Batch(100))
                {
                    var batchOperation = new TableBatchOperation();
                    foreach (var azureLoggingEvent in batch.Select(GetLogEntity))
                    {
                        batchOperation.Insert(azureLoggingEvent);
                    }
                    _table.ExecuteBatch(batchOperation);
                }
            }
        }

        private ITableEntity GetLogEntity(LoggingEvent @event)
        {
            if (Layout != null)
            {
                return new AzureLayoutLoggingEventEntity(@event, PartitionKeyType, Layout);
            }

            return PropAsColumn
                ? (ITableEntity)new AzureDynamicLoggingEventEntity(@event, PartitionKeyType)
                : new AzureLoggingEventEntity(@event, PartitionKeyType);
        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            _account = CloudStorageAccount.Parse(ConnectionString);
            _client = _account.CreateCloudTableClient();
            _table = _client.GetTableReference(TableName);
            _table.CreateIfNotExists();
        }
    }
}