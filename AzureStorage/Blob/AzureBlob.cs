using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob
{
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly CloudBlobClient _blobClient;

        public AzureBlobStorage(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();

        }

        public void SaveBlob(string container, string key, Stream bloblStream)
        {
            var containerRef = _blobClient.GetContainerReference(container);
            containerRef.CreateIfNotExists();

            var blockBlob = containerRef.GetBlockBlobReference(key);

            bloblStream.Position = 0;
            blockBlob.UploadFromStream(bloblStream);
        }

        public Task SaveBlobAsync(string container, string key, Stream bloblStream)
        {
            var containerRef = _blobClient.GetContainerReference(container);
            containerRef.CreateIfNotExists();

            var blockBlob = containerRef.GetBlockBlobReference(key);

            bloblStream.Position = 0;
            return blockBlob.UploadFromStreamAsync(bloblStream);
        }

        public Task SaveBlobAsync(string container, string key, byte[] blob)
        {
            var containerRef = _blobClient.GetContainerReference(container);
            containerRef.CreateIfNotExists();

            var blockBlob = containerRef.GetBlockBlobReference(key);
            return blockBlob.UploadFromByteArrayAsync(blob, 0, blob.Length);
        }


        public Stream this[string container, string key]
        {
            get
            {
                var containerRef = _blobClient.GetContainerReference(container);

                var blockBlob = containerRef.GetBlockBlobReference(key);
                var ms = new MemoryStream();
                blockBlob.DownloadToStream(ms);
                ms.Position = 0;
                return ms;
            }
        }

        public async Task<Stream> GetAsync(string blobContainer, string key)
        {
            var containerRef = _blobClient.GetContainerReference(blobContainer);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            var ms = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(ms);
            ms.Position = 0;
            return ms;
        }

        public string[] FindNamesByPrefix(string container, string prefix)
        {
            var containerRef = _blobClient.GetContainerReference(container);

            var res = containerRef.ListBlobs(prefix, true);
            try
            {
                return res.Select(item => item.Uri.ToString()).ToArray();

            }
            catch (StorageException e)
            {
                if (e.Message.IndexOf("404", StringComparison.Ordinal)>-1)
                  return new string[0];

                throw;
            }
        }

        public IEnumerable<string> GetListOfBlobs(string container)
        {
            var containerRef = _blobClient.GetContainerReference(container);

            BlobContinuationToken token = null;
            do
            {
                var result = containerRef.ListBlobsSegmented(token);
                token = result.ContinuationToken;
                foreach (var listBlobItem in result.Results)
                {
                    yield return listBlobItem.Uri.ToString();
                }
 
                //Now do something with the blobs
            } while (token != null);

        }


        public void DelBlob(string container, string key)
        {
            var containerRef = _blobClient.GetContainerReference(container);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            blockBlob.Delete();
        }

        public Task DelBlobAsync(string blobContainer, string key)
        {
            var containerRef = _blobClient.GetContainerReference(blobContainer);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            return blockBlob.DeleteAsync();
        }

    }
}
