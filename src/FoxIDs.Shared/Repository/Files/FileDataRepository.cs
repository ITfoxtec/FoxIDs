using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class FileDataRepository
    {
        private readonly Settings settings;

        public FileDataRepository(Settings settings)
        {
            this.settings = settings;
            if (settings.Options.DataStorage == DataStorageOptions.File)
            {
                if (!Directory.Exists(GetDbPath()))
                {
                    Directory.CreateDirectory(GetDbPath());
                }
            }
            if (settings.Options.Cache == CacheOptions.File)
            {
                if (!Directory.Exists(GetCachePath()))
                {
                    Directory.CreateDirectory(GetCachePath());
                }
            }
        }

        public async ValueTask<bool> ExistsAsync(string id, string partitionId)
        {
            var data = await GetAsync(id, partitionId, required: false);
            return data != null;
        }

        public ValueTask<int> CountAsync(string partitionId, string dataType)
        {
            if (dataType.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            var count = 0;
            var filePaths = Directory.GetFiles(GetDbPath());
            foreach (string filePath in filePaths)
            {
                var filePathSplit = filePath.Split(Path.DirectorySeparatorChar);
                if (partitionId.IsNullOrWhiteSpace())
                {
                    filePathSplit = filePathSplit[filePathSplit.Length - 1].Split('|');
                }

                if (filePathSplit[filePathSplit.Length - 1].StartsWith(GetFilePartitionIdAndDataType(partitionId, dataType), StringComparison.Ordinal))
                {
                    count++;
                }
            }             
            return ValueTask.FromResult(count);
        }

        public async ValueTask<string> GetAsync(string id, string partitionId, bool required = true, bool delete = false)
        {
            var filePath = await GetFilePathAsync(id, partitionId);

            if (!File.Exists(filePath))
            {
                if (required)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
                else
                {
                    return null;
                }
            }

            var data = await ReadData(filePath, delete);
            if (data == null && required)
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }

            return data;
        }

        private async ValueTask<string> ReadData(string filePath, bool delete = false)
        {
            var data = await File.ReadAllTextAsync(filePath);
            if (IsValid(filePath, data))
            {
                if (delete)
                {
                    File.Delete(filePath);
                }
                return data;
            }
            else
            {
                File.Delete(filePath);
            }
            return null;
        }

        public async ValueTask<List<string>> GetListAsync(string partitionId, string dataType, int? pageSize = null)
        {
            if(dataType.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            var filePaths = Directory.GetFiles(GetDbPath());
            var selectedFilePaths = new List<string>();
            foreach (string filePath in filePaths)
            {
                var filePathSplit = filePath.Split(Path.DirectorySeparatorChar);
                if (partitionId.IsNullOrWhiteSpace())
                {
                    filePathSplit = filePathSplit[filePathSplit.Length - 1].Split('|');
                }

                if (filePathSplit[filePathSplit.Length - 1].StartsWith(GetFilePartitionIdAndDataType(partitionId, dataType), StringComparison.Ordinal))
                {
                    selectedFilePaths.Add(filePath);
                }
            }

            var dataItems = new List<string>();
            var count = 0;
            foreach (string filePath in selectedFilePaths)
            {
                var data = await ReadData(filePath);
                if(data != null)
                {
                    dataItems.Add(data);
                }
                count++;
                if (pageSize.HasValue && count >= pageSize.Value)
                {
                    break;
                }
            }

            return dataItems;
        }

        public async ValueTask CreateAsync(string id, string partitionId, string item)
        {
            var filePath = await GetFilePathAsync(id, partitionId);
            if (File.Exists(filePath))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.Conflict };
            }

            await File.WriteAllTextAsync(filePath, item);
        }

        public async ValueTask UpdateAsync(string id, string partitionId, string item)
        {
            var filePath = await GetFilePathAsync(id, partitionId);
            if (!File.Exists(filePath))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }

            await File.WriteAllTextAsync(filePath, item);
        }

        public async ValueTask SaveAsync(string id, string partitionId, string item)
        {
            var filePath = await GetFilePathAsync(id, partitionId);

            await File.WriteAllTextAsync(filePath, item);
        }

        public async ValueTask DeleteAsync(string id, string partitionId, bool required = true)
        {
            var filePath = await GetFilePathAsync(id, partitionId);
            if (!File.Exists(filePath))
            {
                if (required)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
                else
                {
                    return;
                }
            }

            File.Delete(filePath);
        }

        public ValueTask<long> DeleteListAsync(string partitionId, string dataType)
        {
            if (dataType.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            long count = 0;
            foreach (string filePath in Directory.GetFiles(GetDbPath()))
            {
                var filePathSplit = filePath.Split(Path.DirectorySeparatorChar);
                if (filePathSplit[filePathSplit.Length - 1].StartsWith(GetFilePartitionIdAndDataType(partitionId, dataType), StringComparison.Ordinal))
                {
                    File.Delete(filePath);
                    count++;
                }
            }
            return ValueTask.FromResult(count);
        }

        public async Task CleanDataAsync(CancellationToken stoppingToken)
        {
            if (settings.Options.DataStorage == DataStorageOptions.File)
            {
                await CleanDataAsync(Directory.GetFiles(GetDbPath()), stoppingToken);
            }
            if (settings.Options.Cache == CacheOptions.File)
            {
                await CleanDataAsync(Directory.GetFiles(GetCachePath()), stoppingToken);
            }
        }

        private async Task CleanDataAsync(string[] filePaths, CancellationToken stoppingToken)
        {
            foreach (string filePath in filePaths)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                _ = await ReadData(filePath);
            }
        }

        private bool IsValid(string filePath, string data)
        {
            var ttlDocument = data.DataJsonToObject<DataTtlDocument>();
            if (ttlDocument.TimeToLive == 0)
            {
                return true;
            }

            var lastUpdated = File.GetLastWriteTimeUtc(filePath);
            if (lastUpdated.AddSeconds(ttlDocument.TimeToLive) >= DateTimeOffset.UtcNow)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<string> GetFilePathAsync(string id, string partitionId)
        {
            var idSplit = id.Split(':');
            return $"{GetPath(idSplit)}{Path.DirectorySeparatorChar}{GetFilePartitionId(partitionId)}^{GetPre(id, idSplit)}{await id.HashIdStringAsync()}.data";
        }

        private string GetPre(string id, string[] idSplit)
        {
            if (id.StartsWith(Constants.Models.DataType.Party, StringComparison.Ordinal))
            {
                return $"{idSplit[0]}_{idSplit[1]}-";
            }
            else
            {
                return $"{idSplit[0]}-";
            }
        }

        private string GetPath(string[] idSplit)
        {            
            if (idSplit[0].Equals(Constants.Models.DataType.Cache, StringComparison.Ordinal))
            {
                return GetCachePath();
            }
            else
            {
                return GetDbPath();
            }
        }

        private string GetFilePartitionIdAndDataType(string partitionId, string dataType)
        {
            return $"{(partitionId.IsNullOrWhiteSpace() ? string.Empty : $"{GetFilePartitionId(partitionId)}")}^{dataType.Replace(':', '_')}";
        }

        private string GetFilePartitionId(string partitionId) => partitionId.Replace(':', '_');

        private string GetDataPath() => Path.Join(settings.FileData.DataPath, "data");
        private string GetDbPath() => Path.Join(GetDataPath(), "db");
        private string GetCachePath() => Path.Join(GetDataPath(), Constants.Models.DataType.Cache);
    }
}
