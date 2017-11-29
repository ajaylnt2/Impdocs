using AssetModel;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AssetServices
{
    public class AssetManager
    {
        //Read config

        private const string collectionName = "AssetCollection";
        private const string databaseName = "Asset";
        private const string EndpointLocal = "https://localhost:8081";
        private const string EndpointUrl = "https://yasvanthdb.documents.azure.com:443/";
        private const string PrimaryKey = "WpFXjPyJbShrEjxBpzF0zYgT0TOiaJGtpGxUtKiqyM3D8Al2g2mvmJocibpI0Mt0OeZ83L0P56cfD2kflSF0XQ==";
        private DocumentClient client;
        private Uri documentUri;
        private FeedOptions queryOptions;

        //Reusable instance of DocumentClient which represents the connection to a DocumentDB endpointurl and primarykey..
        public AssetManager()
        { 
            //Get a Document client

            this.client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
            GetDbInit().Wait();
            queryOptions = new FeedOptions { MaxItemCount = -1 };
            documentUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);
        }
        // Create Document if doesnot exists..
        public async Task<Asset> CreateAssetDocumentIfNotExists(Asset asset)
        {
         
            try
            {
                await GetAssetById(asset.AssetId);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                  await client.CreateDocumentAsync(documentUri, asset);
                }
            }
            return null;
        }

        // delete the asset based on id..
        public async Task DeleteAssetbyId(string id)
        {
            await this.client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName,collectionName, id));
        }

        // fetch the asset based on Id
        public async Task<Asset> GetAssetById(string assetId)
        {
            return await this.client.ReadDocumentAsync<Asset>(UriFactory.CreateDocumentUri(databaseName, collectionName, assetId));
        }
        // fetch  all assets..
        public List<Asset> GetAssets()
        {
            var data = this.client.CreateDocumentQuery<Asset>(documentUri, queryOptions).Select(f => f).ToList();
            return data;
        }
        // Edit the asset 
        internal void EditAsset(Asset asset)
        {
        }
        // fetch the asset type based on the asset type..
        internal List<Asset> GetAssetByType(string assetType)
        {
            var data = this.client.CreateDocumentQuery<Asset>(documentUri, queryOptions).Where(f => f.AssetType == assetType).ToList();
            return data;
        }
        // fetch the Tagdetails based on tagId...
        internal TagDetails GetTagDetails(int tagId)
        {
            var tag = GetTag(tagId);
            if(tag!=null)
            {
                var details = tag.TagDetails;
                if(details !=null)
                {
                    return details;
                }
            }
            return null;
        }

        // fetch the tag based on Id
        internal Tag GetTag(int tagId)
        {
            var assets = GetAssets();

            foreach (var asset in assets)
            {
                var tags = asset.TagCollection;
                if(tags!=null && tags.Count > 0)
                {
                    var tag = tags.FirstOrDefault(x => x.TagId == tagId);
                    if(tag !=null)
                    {
                        return tag;
                    }
                }
            }

            return null;
        }
        //Get asset datasource based on assetId...
        internal async Task<DataSource> GetAssetDataSource(string assetId)
        {
            var asset = await GetAssetById(assetId);
            if (asset != null)
            {
                return asset.DataSource;
            }
            return null;
        }
        // fetch all asset tags based on the assetId
        internal async Task<IEnumerable<Tag>> GetAssetTags(string assetId)
        {
            var asset = await GetAssetById(assetId);
            if (asset != null)
            {
                return asset.TagCollection;
            }
            return null;
        }
        // create the Database and  document collections if not exists
        private async Task GetDbInit()
        {
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
            await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), new DocumentCollection { Id = collectionName });
        }
    }
}