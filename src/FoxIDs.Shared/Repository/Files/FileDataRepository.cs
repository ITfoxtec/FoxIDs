using FoxIDs.Models;
using FoxIDs.Models.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Console.WriteLine("ExistsAsync " + id);

            var data = await GetAsync(id, partitionId, required: false);
            return data != null;
        }

        public ValueTask<int> CountAsync(string partitionId)
        {
            Console.WriteLine("CountAsync partition " + partitionId);

            var count = 0;
            var filePaths = Directory.GetFiles(GetDbPath());
            foreach (string filePath in filePaths)
            {
                var filePathSplit = filePath.Split('\\');
                if (filePathSplit[filePathSplit.Length - 1].StartsWith(GetFilePartitionId(partitionId), StringComparison.Ordinal))
                {
                    count++;
                }
            }             
            return ValueTask.FromResult(count);
        }

        public async ValueTask<string> GetAsync(string id, string partitionId, bool required = true, bool delete = false)
        {
            Console.WriteLine("GetAsync " + id);

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

        public async ValueTask<List<string>> GetListAsync(string partitionId, int? maxItemCount = null)
        {
            var filePaths = Directory.GetFiles(GetDbPath());
            var selectedFilePaths = partitionId == null ? filePaths.ToList() : new List<string>();
            if (partitionId != null)
            {
                foreach (string filePath in filePaths)
                {
                    var filePathSplit = filePath.Split('\\');
                    if (filePathSplit[filePathSplit.Length - 1].StartsWith(GetFilePartitionId(partitionId), StringComparison.Ordinal))
                    {
                        selectedFilePaths.Add(filePath);
                    }
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
                if (maxItemCount.HasValue && count >= maxItemCount.Value)
                {
                    break;
                }
            }

            return dataItems;
        }

        public async ValueTask CreateAsync(string id, string partitionId, string item)
        {
            Console.WriteLine("CreateAsync " + id);

            var filePath = await GetFilePathAsync(id, partitionId);
            if (File.Exists(filePath))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.Conflict };
            }

            await File.WriteAllTextAsync(filePath, item);
        }

        public async ValueTask UpdateAsync(string id, string partitionId, string item)
        {
            Console.WriteLine("UpdateAsync " + id);

            var filePath = await GetFilePathAsync(id, partitionId);
            if (!File.Exists(filePath))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }

            await File.WriteAllTextAsync(filePath, item);
        }

        public async ValueTask SaveAsync(string id, string partitionId, string item)
        {
            Console.WriteLine("SaveAsync " + id);

            var filePath = await GetFilePathAsync(id, partitionId);

            await File.WriteAllTextAsync(filePath, item);
        }

        public async ValueTask<string> DeleteAsync(string id, string partitionId, bool required = true)
        {
            Console.WriteLine("DeleteAsync " + id);

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

            var data = await ReadData(filePath);
            File.Delete(filePath);
            return data;
        }

        public ValueTask<int> DeleteListAsync(string partitionId)
        {
            var count = 0;
            foreach (string filePath in Directory.GetFiles(GetDbPath()))
            {
                var filePathSplit = filePath.Split('\\');
                if (filePathSplit[filePathSplit.Length - 1].StartsWith(GetFilePartitionId(partitionId), StringComparison.Ordinal))
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
            return $"{GetPath(idSplit)}\\{GetFilePartitionId(partitionId)}-{GetPre(id, idSplit)}{await id.HashIdStringAsync()}.data";
        }

        private string GetPre(string id, string[] idSplit)
        {
            if (id.StartsWith(Constants.Models.DataType.Party, StringComparison.Ordinal))
            {
                return $"{idSplit[0]}_{idSplit[1]}-";
            }
            else if (!id.StartsWith(Constants.Models.DataType.Tenant, StringComparison.Ordinal) && !id.StartsWith(Constants.Models.DataType.Track, StringComparison.Ordinal) && !id.StartsWith(Constants.Models.DataType.Cache, StringComparison.Ordinal))
            {
                return $"{idSplit[0]}-";
            }
            else
            {
                return string.Empty;
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

        private string GetFilePartitionId(string partitionId) => partitionId.Replace(':', '_');

        private string GetDataPath() => $"{settings.FileData.DataPath.TrimEnd('\\')}\\data";
        private string GetDbPath() => $"{GetDataPath()}\\db";
        private string GetCachePath() => $"{GetDataPath()}\\{Constants.Models.DataType.Cache}";
    }
}
