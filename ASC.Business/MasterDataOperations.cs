using ASC.Business.Interfaces;
using ASC.DataAccess;
using ASC.Model.Models;
using ASC.Model;

namespace ASC.Business
{
    public class MasterDataOperations : IMasterDataOperations
    {
        private readonly IUnitOfWork _unitOfWork;

        public MasterDataOperations(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<MasterDataKey>> GetAllMasterKeysAsync()
        {
            var masterKeys = await _unitOfWork.Repository<MasterDataKey>().FindAllAsync();
            return masterKeys.ToList();
        }

        public async Task<List<MasterDataKey>> GetMaserKeyByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<MasterDataKey>();
            }

            var masterKeys = await _unitOfWork.Repository<MasterDataKey>()
                .FindAllByPartitionKeyAsync(name.Trim());

            return masterKeys.ToList();
        }

        public async Task<bool> InsertMasterKeyAsync(MasterDataKey key)
        {
            if (key == null)
            {
                return false;
            }

            key.PartitionKey = key.PartitionKey?.Trim();
            key.RowKey = string.IsNullOrWhiteSpace(key.RowKey)
                ? Guid.NewGuid().ToString()
                : key.RowKey.Trim();

            key.Name = string.IsNullOrWhiteSpace(key.Name)
                ? key.PartitionKey
                : key.Name.Trim();

            key.IsDeleted = false;

            if (string.IsNullOrWhiteSpace(key.CreatedBy))
            {
                key.CreatedBy = "System";
            }

            if (string.IsNullOrWhiteSpace(key.UpdatedBy))
            {
                key.UpdatedBy = key.CreatedBy;
            }

            await _unitOfWork.Repository<MasterDataKey>().AddAsync(key);
            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<bool> UpdateMasterKeyAsync(string orginalPartitionKey, MasterDataKey key)
        {
            if (key == null)
            {
                return false;
            }

            var masterKey = await _unitOfWork.Repository<MasterDataKey>()
                .FindAsync(orginalPartitionKey, key.RowKey);

            if (masterKey == null)
            {
                return false;
            }

            masterKey.IsActive = key.IsActive;
            masterKey.IsDeleted = key.IsDeleted;
            masterKey.Name = key.Name;
            masterKey.UpdatedBy = key.UpdatedBy;

            _unitOfWork.Repository<MasterDataKey>().Update(masterKey);
            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new List<MasterDataValue>();
            }

            var masterValues = await _unitOfWork.Repository<MasterDataValue>()
                .FindAllByPartitionKeyAsync(key.Trim());

            return masterValues
                .Where(x => !x.IsDeleted)
                .ToList();
        }

        public async Task<MasterDataValue> GetMasterValueByNameAsync(string key, string name)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var masterValues = await GetAllMasterValuesByKeyAsync(key);

            return masterValues.FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> InsertMasterValueAsync(MasterDataValue value)
        {
            if (value == null)
            {
                return false;
            }

            var masterKey = await GetMasterKeyAsync(value.PartitionKey);

            if (masterKey == null)
            {
                return false;
            }

            PrepareMasterDataValue(value, masterKey);

            await _unitOfWork.Repository<MasterDataValue>().AddAsync(value);
            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<bool> UpdateMasterValueAsync(
            string originalPartitionKey,
            string originalRowKey,
            MasterDataValue value)
        {
            if (value == null)
            {
                return false;
            }

            var masterValue = await _unitOfWork.Repository<MasterDataValue>()
                .FindAsync(originalPartitionKey, originalRowKey);

            if (masterValue == null)
            {
                return false;
            }

            var masterKey = await GetMasterKeyAsync(value.PartitionKey);

            if (masterKey == null)
            {
                return false;
            }

            masterValue.PartitionKey = value.PartitionKey.Trim();
            masterValue.Name = value.Name.Trim();
            masterValue.Value = string.IsNullOrWhiteSpace(value.Value)
                ? value.Name.Trim()
                : value.Value.Trim();

            masterValue.IsActive = value.IsActive;
            masterValue.IsDeleted = false;
            masterValue.UpdatedBy = value.UpdatedBy;

            masterValue.MasterDataKeyPartitionKey = masterKey.PartitionKey;
            masterValue.MasterDataKeyRowKey = masterKey.RowKey;

            _unitOfWork.Repository<MasterDataValue>().Update(masterValue);
            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesAsync()
        {
            var masterValues = await _unitOfWork.Repository<MasterDataValue>().FindAllAsync();

            return masterValues
                .Where(x => !x.IsDeleted)
                .ToList();
        }

        public async Task<bool> UploadBulkMasterData(List<MasterDataValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return false;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value.PartitionKey) ||
                    string.IsNullOrWhiteSpace(value.Name))
                {
                    continue;
                }

                value.PartitionKey = value.PartitionKey.Trim();
                value.Name = value.Name.Trim();

                var masterKey = await GetMasterKeyAsync(value.PartitionKey);

                if (masterKey == null)
                {
                    masterKey = new MasterDataKey
                    {
                        PartitionKey = value.PartitionKey,
                        RowKey = Guid.NewGuid().ToString(),
                        Name = value.PartitionKey,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedBy = string.IsNullOrWhiteSpace(value.CreatedBy) ? "System" : value.CreatedBy,
                        UpdatedBy = string.IsNullOrWhiteSpace(value.UpdatedBy) ? "System" : value.UpdatedBy
                    };

                    await _unitOfWork.Repository<MasterDataKey>().AddAsync(masterKey);
                    _unitOfWork.CommitTransaction();
                }

                PrepareMasterDataValue(value, masterKey);

                var masterValuesByKey = await GetAllMasterValuesByKeyAsync(value.PartitionKey);

                var existingValue = masterValuesByKey.FirstOrDefault(x =>
                    string.Equals(x.Name, value.Name, StringComparison.OrdinalIgnoreCase));

                if (existingValue == null)
                {
                    await _unitOfWork.Repository<MasterDataValue>().AddAsync(value);
                }
                else
                {
                    existingValue.IsActive = value.IsActive;
                    existingValue.IsDeleted = false;
                    existingValue.Name = value.Name;
                    existingValue.Value = value.Value;
                    existingValue.UpdatedBy = value.UpdatedBy;
                    existingValue.MasterDataKeyPartitionKey = masterKey.PartitionKey;
                    existingValue.MasterDataKeyRowKey = masterKey.RowKey;

                    _unitOfWork.Repository<MasterDataValue>().Update(existingValue);
                }
            }

            _unitOfWork.CommitTransaction();

            return true;
        }

        private async Task<MasterDataKey> GetMasterKeyAsync(string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                return null;
            }

            var masterKeys = await _unitOfWork.Repository<MasterDataKey>()
                .FindAllByPartitionKeyAsync(partitionKey.Trim());

            return masterKeys.FirstOrDefault();
        }

        private static void PrepareMasterDataValue(MasterDataValue value, MasterDataKey masterKey)
        {
            value.PartitionKey = value.PartitionKey.Trim();
            value.RowKey = string.IsNullOrWhiteSpace(value.RowKey)
                ? Guid.NewGuid().ToString()
                : value.RowKey.Trim();

            value.Name = value.Name.Trim();

            value.Value = string.IsNullOrWhiteSpace(value.Value)
                ? value.Name
                : value.Value.Trim();

            value.IsDeleted = false;

            if (string.IsNullOrWhiteSpace(value.CreatedBy))
            {
                value.CreatedBy = "System";
            }

            if (string.IsNullOrWhiteSpace(value.UpdatedBy))
            {
                value.UpdatedBy = value.CreatedBy;
            }

            value.MasterDataKeyPartitionKey = masterKey.PartitionKey;
            value.MasterDataKeyRowKey = masterKey.RowKey;
        }
    }
}