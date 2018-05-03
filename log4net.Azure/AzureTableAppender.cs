using log4net.Appender.Extensions;
using log4net.Appender.Language;
using log4net.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;

namespace log4net.Appender
{
    /// <summary>
    /// Azure Table appender for log4net
    /// </summary>
    public class AzureTableAppender : BufferingAppenderSkeleton
    {
        private CloudStorageAccount _account;
        private CloudTableClient _client;
        private CloudTable _table;
        /// <summary>
        /// Connection string name
        /// </summary>
        public string ConnectionStringName { get; set; }

        private string _connectionString;
        /// <summary>
        /// Connection string of the Azure Table
        /// </summary>
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
        /// <summary>
        /// Table name to store events
        /// </summary>
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
        /// <summary>
        /// Properties as Colummns
        /// </summary>
        public bool PropAsColumn { get; set; }

	    private PartitionKeyTypeEnum _partitionKeyType = PartitionKeyTypeEnum.LoggerName;
        /// <summary>
        /// Enumeration for different partition key types
        /// </summary>
        public PartitionKeyTypeEnum PartitionKeyType
        {
            get { return _partitionKeyType; }
            set { _partitionKeyType = value; }
        }

        /// <summary>
        /// Push events to Azure Table
        /// </summary>
        /// <param name="events">Buffered events in log4net</param>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            var grouped = events.Select(GetLogEntity).GroupBy(evt => evt.PartitionKey);

            foreach (var group in grouped)
            {
                foreach (var batch in group.Batch(100))
                {
                    var batchOperation = new TableBatchOperation();
                    foreach (var azureLoggingEvent in batch)
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

        /// <summary>
        /// Activate options for log4net
        /// </summary>
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