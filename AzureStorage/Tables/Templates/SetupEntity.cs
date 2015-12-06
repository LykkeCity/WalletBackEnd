using System.Threading.Tasks;
using Common;
using Common.Log;

namespace AzureStorage.Tables.Templates
{

    public interface INoSqlTableForSetup
    {

        T GetValue<T>(string field, T defaultValue);
        void SetValue<T>(string field, T value);

        Task<T> GetValueAsync<T>(string field, T defaultValue);
        Task SetValueAsync<T>(string field, T value);

    }

    public abstract class NoSqlTableForSetupAbstract : INoSqlTableForSetup
    {
        private readonly NoSqlSetupByPartition _tableStorage;

        protected NoSqlTableForSetupAbstract(NoSqlSetupByPartition tableStorage)
        {
            _tableStorage = tableStorage;
            Partition = DefaultPartition;
        }

        public const string DefaultPartition = "Setup";
        public string Partition { get; set; }
        public string Field { get; set; }

        public string this[string field]
        {
            get
            {
                return _tableStorage.GetValue(Partition, field);

            }
            set
            {
                _tableStorage.SetValue(Partition, field, value);
            }
        }

        public T GetValue<T>(string field, T defaultValue)
        {
            return _tableStorage.GetValue(Partition, field, defaultValue);
        }

        public void SetValue<T>(string field, T value)
        {
            _tableStorage.SetValue(Partition, field, value);
        }

        public Task<T> GetValueAsync<T>(string field, T defaultValue)
        {
            return _tableStorage.GetValueAsync(Partition, field, defaultValue);
        }

        public Task SetValueAsync<T>(string field, T value)
        {
            return _tableStorage.SetValueAsync(Partition, field, value);
        }
    }

    public class NoSqlTableForSetup : NoSqlTableForSetupAbstract
    {
        public NoSqlTableForSetup(string connStr, string tableName, ILog log, bool caseSensitive = true) :
            base(new AzureSetupByPartition(connStr, tableName, log, caseSensitive))
        {
        }

    }

    public class NoSqlTableForSetupInMemory : NoSqlTableForSetupAbstract
    {
        public NoSqlTableForSetupInMemory() :
            base(new NoSqlSetupByPartitionInMemory())
        {
        }

    }

}
