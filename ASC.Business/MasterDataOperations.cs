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

            return masterKeys
                .GroupBy(x => x.PartitionKey)
                .Select(g => g.First())
                .ToList();
        }

        public async Task<List<MasterDataKey>> GetMaserKeyByNameAsync(string name)
        {
            var masterKeys = await _unitOfWork.Repository<MasterDataKey>()
                .FindAllByPartitionKeyAsync(name);
            return masterKeys.ToList();
        }

        public async Task<bool> InsertMasterKeyAsync(MasterDataKey key)
        {
            using (_unitOfWork)
            {
                await _unitOfWork.Repository<MasterDataKey>().AddAsync(key);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }

        public async Task<bool> UpdateMasterKeyAsync(string orginalPartitionKey, MasterDataKey key)
        {
            var masterKey = await _unitOfWork.Repository<MasterDataKey>()
                .FindAsync(orginalPartitionKey, key.RowKey);
            if (masterKey == null) return false;
            masterKey.IsActive = key.IsActive;
            masterKey.IsDeleted = key.IsDeleted;
            masterKey.Name = key.Name;
            _unitOfWork.Repository<MasterDataKey>().Update(masterKey);
            _unitOfWork.CommitTransaction();
            return true;
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesByKeyAsync(string key)
        {
            try
            {
                var masterValues = await _unitOfWork.Repository<MasterDataValue>()
                    .FindAllByPartitionKeyAsync(key);

                return masterValues.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<MasterDataValue>();
            }
        }

        public async Task<MasterDataValue> GetMasterValueByNameAsync(string key, string name)
        {
            var masterValues = await _unitOfWork.Repository<MasterDataValue>()
                .FindAsync(key, name);

            return masterValues;
        }

        public async Task<bool> InsertMasterValueAsync(MasterDataValue value)
        {
            using (_unitOfWork)
            {
                await _unitOfWork.Repository<MasterDataValue>().AddAsync(value);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }

        public async Task<bool> UpdateMasterValueAsync(string originalPartitionKey, string originalRowKey, MasterDataValue value)
        {
            using (_unitOfWork)
            {
                var masterValue = await _unitOfWork.Repository<MasterDataValue>()
                    .FindAsync(originalPartitionKey, originalRowKey);

                if (masterValue == null) return false;

                masterValue.IsActive = value.IsActive;
                masterValue.IsDeleted = value.IsDeleted;
                masterValue.Name = value.Name;

                _unitOfWork.Repository<MasterDataValue>().Update(masterValue);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesAsync()
        {
            var masterValues = await _unitOfWork.Repository<MasterDataValue>().FindAllAsync();
            return masterValues.ToList();
        }

        public async Task<bool> UploadBulkMasterData(List<MasterDataValue> values)
        {
            if (values == null || values.Count == 0)
                return false;

            var allValues = await GetAllMasterValuesAsync();

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value.PartitionKey) ||
                    string.IsNullOrWhiteSpace(value.Name))
                    continue;

                value.PartitionKey = value.PartitionKey.Trim();
                value.Name = value.Name.Trim();

                var masterKey = await GetMasterKeyAsync(value.PartitionKey);

                if (masterKey == null)
                {
                    var existingKey = await GetMasterKeyAsync(value.PartitionKey);

                    if (existingKey == null)
                    {
                        masterKey = new MasterDataKey
                        {
                            PartitionKey = value.PartitionKey,
                            RowKey = Guid.NewGuid().ToString(),
                            Name = value.PartitionKey,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = value.CreatedBy ?? "System",
                            UpdatedBy = value.UpdatedBy ?? "System"
                        };

                        await _unitOfWork.Repository<MasterDataKey>().AddAsync(masterKey);
                    }
                    else
                    {
                        masterKey = existingKey;
                    }
                }

                PrepareMasterDataValue(value, masterKey);

                var existing = allValues.FirstOrDefault(x =>
                    x.PartitionKey == value.PartitionKey &&
                    x.Name.Equals(value.Name, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    await _unitOfWork.Repository<MasterDataValue>().AddAsync(value);
                    allValues.Add(value); //tránh trùng
                }
                else
                {
                    existing.IsActive = value.IsActive;
                    existing.IsDeleted = false;
                    existing.Name = value.Name;
                    existing.Value = value.Value;
                    existing.UpdatedBy = value.UpdatedBy;

                    _unitOfWork.Repository<MasterDataValue>().Update(existing);
                }
            }

            _unitOfWork.CommitTransaction();
            return true;
        }
        private async Task<MasterDataKey> GetMasterKeyAsync(string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
                return null;

            partitionKey = partitionKey.Trim();

            var keys = await _unitOfWork.Repository<MasterDataKey>()
                .FindAllByPartitionKeyAsync(partitionKey);

            return keys.FirstOrDefault();
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
