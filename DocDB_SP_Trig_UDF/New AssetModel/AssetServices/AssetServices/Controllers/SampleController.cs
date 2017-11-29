using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.IO;
using AssetModel;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace AssetServices.Controllers
{
    [Produces("application/json")]
    //[Route("api/[controller]")]
    public class SampleController : Controller
    {
        const string Endpoint = "https://saicosmosdb.documents.azure.com:443/";
        const string AuthKey = "mREWu5HZzAlmt2wQYkamIUeyHVxnBVswa1YlQUuv9hhyXDbALmd45UxCyjaL5147WC9Zah2U6966IX6hG3QT7Q==";

        const string DbName = "Asset";
        const string CollectionName = "Assetcollection";

        private static object client;
        private static string triggerId;

        //Reusable instance of DocumentClient which represents the connection to a DocumentDB endpointurl and primarykey..
        [Route("api/Sample")]
        public async Task<IActionResult> Run()
        {
            
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);

                await CreateTriggers(client);

                await Execute_trgEnsureUniqueId(client, collectionLink);
                await Executed_trgUpdateMetaData(client, collectionLink);
                await Executed_canonicalizeSchedule(client, collectionLink);

                await CreateUserDefinedFunctions(client);

                Execute_udfRegEx(client);
                Execute_udfIsMysore(client);
                
            }

            return Ok();
        }



        //Execute the udf
        private void Execute_udfIsMysore(DocumentClient client)
        {
            Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);
            var sql = "select c.id, c.Location from c where udf.udfIsMysore(c.Location) = true";

            var documents = client.CreateDocumentQuery(collectionLink, sql).ToList();

            Console.WriteLine();
            Console.WriteLine("////Found {0} Mysore Assets////", documents.Count);
            foreach (var item in documents)
            {
                Console.WriteLine("{0}, {1}", item.id, item.Location);
            }

            sql = "select c.id, c.Location from c where udf.udfIsMysore(c.Location) = false";

            documents = client.CreateDocumentQuery(collectionLink, sql).ToList();

            Console.WriteLine();
            Console.WriteLine("///Found {0} non Mysore Assets////", documents.Count);
            foreach (var item in documents)
            {
                Console.WriteLine("{0}, {1}", item.id, item.Location);
            }
            Console.WriteLine();
            Console.WriteLine();
        }
        // Execute in the udfRegEx
        private void Execute_udfRegEx(DocumentClient client)
        {
            Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);
            // query 
            var sql = "select c.id from c where udf.udfRegEx(c.AssetType, 'Plant') != null";

            var documents = client.CreateDocumentQuery(collectionLink, sql).ToList();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("////Found {0} Assets who has asset type as Plant////", documents.Count);
            foreach (var item in documents)
            {
                Console.WriteLine("{0}", item.id);
            }
            Console.WriteLine();
        }
        // Creating the UDF..
        private async static Task CreateUserDefinedFunctions(DocumentClient client)
        {
            await CreateUdfIfNotExists(client, "udfRegEx");
            await CreateUdfIfNotExists(client, "udfIsMysore");
            
        }

        // Create the UDF if does not exists..
        private async static Task CreateUdfIfNotExists(DocumentClient client, string udfId)
        {
            Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);

            var udfBody = System.IO.File.ReadAllText(@"" + udfId + ".js");

            var udfDefinition = new UserDefinedFunction
            {
                Id = udfId,
                Body = udfBody
            };

            bool needToCreate = false;
            Uri udfUri = UriFactory.CreateUserDefinedFunctionUri(DbName, CollectionName, udfId);

            try
            {
                await client.ReadUserDefinedFunctionAsync(udfUri);
            }
            catch (DocumentClientException dce)
            {
                if (dce.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
                else
                {
                    needToCreate = true;
                }
            }
            if (needToCreate)
            {
                await client.CreateUserDefinedFunctionAsync(collectionLink, udfDefinition);
            }
        }

        // Stored Procedure to get all assets
        [HttpGet]
        [Route("api/Sample/Get")]
        public async Task<IActionResult> GetAssetSproc()
        {
            dynamic data = null;
            try
            {
                string scriptFileName = @"GetAll.js";
                string scriptId = Path.GetFileNameWithoutExtension(scriptFileName);
                string scriptName = "GetAll";

                await CreateSprocIfNotExists(scriptFileName, scriptId, scriptName);

                Uri sprocUri = UriFactory.CreateStoredProcedureUri(DbName, CollectionName, scriptName);
                var client = new DocumentClient(new Uri(Endpoint), AuthKey);
                // execute the procedure
                data = await client.ExecuteStoredProcedureAsync<object>(sprocUri);
            }
            catch (DocumentClientException ex)
            {
                Console.WriteLine("<script>alert('error : " + ex.Message + "');</script>");
            }
            return Ok(data);
        }

        // Stored Procedure to get asset based on the Id
        [HttpGet]
        [Route("api/Sample/Get/{id}")]
        public async Task<IActionResult> GetAssetByIdSproc(string id)
        {
            dynamic data = null;
            try
            {
                string scriptFileName = @"GetById.js";
                string scriptId = Path.GetFileNameWithoutExtension(scriptFileName);
                string scriptName = "GetById";

                await CreateSprocIfNotExists(scriptFileName, scriptId, scriptName);

                Uri sprocUri = UriFactory.CreateStoredProcedureUri(DbName, CollectionName, scriptName);
                var client = new DocumentClient(new Uri(Endpoint), AuthKey);
                // execute the procedure..
                data = await client.ExecuteStoredProcedureAsync<string>(sprocUri, id);
            }
            catch (DocumentClientException ex)
            {
                Console.WriteLine("<script>alert('error : " + ex.Message + "');</script>");
            }
            return Ok(data.Response);
        }

        

        [HttpDelete]
        public async Task<IActionResult> BulkDeleteSproc()
        {
            dynamic data = null;
            var client = new DocumentClient(new Uri(Endpoint), AuthKey);
            Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);

            string scriptFileName = @"Delete.js";
            string scriptId = Path.GetFileNameWithoutExtension(scriptFileName);
            string scriptName = "Delete";

            try
            {
                await CreateSprocIfNotExists(scriptFileName, scriptId, scriptName);
                Uri sprocUri = UriFactory.CreateStoredProcedureUri(DbName, CollectionName, scriptName);

                data = await client.ExecuteStoredProcedureAsync<string>(sprocUri);
            }
            catch (DocumentClientException ex)
            {
                Console.WriteLine("<script>alert('error : " + ex.Message + "');</script>");
            }

            return Ok(data.Response);
        }

        //Create the procedure if doesnot exists..
        private static async Task CreateSprocIfNotExists(string scriptFileName, string scriptId, string scriptName)
        {
            var client = new DocumentClient(new Uri(Endpoint), AuthKey);
            Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);

            var sprocDefinition = new StoredProcedure
            {
                Id = scriptId,
                Body = System.IO.File.ReadAllText(scriptFileName)
            };

            bool needToCreate = false;
            Uri sprocUri = UriFactory.CreateStoredProcedureUri(DbName, CollectionName, scriptName);

            try
            {
                await client.ReadStoredProcedureAsync(sprocUri);
            }
            catch (DocumentClientException dce)
            {
                if (dce.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
                else
                {
                    needToCreate = true;
                }
            }
           
            if (needToCreate)
            {
                await client.CreateStoredProcedureAsync(collectionLink, sprocDefinition);
            }
        }

        //create the pre-triggers and post-triggers
        private async static Task CreateTriggers(DocumentClient client)
        {
            try
            {
                // Create Pre-Trigger
                var trgEnsureUniqueIdBody = System.IO.File.ReadAllText(@"trgEnsureUniqueId.js");
                await CreateTriggerIfNotExists(client, "trgEnsureUniqueId", trgEnsureUniqueIdBody, TriggerType.Pre, TriggerOperation.Create);

                // Create Post-Trigger
                var trgUpdateMetaDataBody = System.IO.File.ReadAllText(@"trgUpdateMetaData.js");
                await CreateTriggerIfNotExists(client, "trgUpdateMetaData", trgUpdateMetaDataBody, TriggerType.Post, TriggerOperation.All);

            }
            catch (Exception ex)
            {
                Console.WriteLine("<script>alert('error : " + ex.Message + "');</script>");
            }
        }


        //create the triggers if desnot exists...
        private async static Task CreateTriggerIfNotExists(DocumentClient client, string triggerId, string triggerBody, TriggerType triggerType, TriggerOperation triggerOperation)
        {
            Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, CollectionName);

            var triggerDefinition = new Trigger
            {
                Id = triggerId,
                Body = triggerBody,
                TriggerType = triggerType,
                TriggerOperation = triggerOperation
            };

            bool needToCreate = false;
            Uri triggerUri = UriFactory.CreateTriggerUri(DbName, CollectionName, triggerId);

            try
            {
                await client.ReadTriggerAsync(triggerUri);
            }
            catch (DocumentClientException dce)
            {
                if (dce.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
                else
                {
                    needToCreate = true;
                }
            }

            if (needToCreate)
            {
               
                await client.CreateTriggerAsync(collectionLink, triggerDefinition);
               
            }

        }
        
        //Excute the post trigger...
        private async static Task Executed_trgUpdateMetaData(DocumentClient client, Uri collectionLink)
        {
            // Creating a new document also updates metadata documeny (or creates it, if it doesn't exist yet)
            dynamic newDoc = new { id = "TrigTestMeta.1", name = "Test Post Trigger" };

            var result = await client.CreateDocumentAsync(collectionLink, newDoc, new RequestOptions { PostTriggerInclude = new[] { "trgUpdateMetaData" } });

            // Retrieve the metadata document (has ID of last inserted data document)
            var metadoc = client.CreateDocumentQuery(collectionLink, "select * from c where c.id = '_metadata'").AsEnumerable().First();
            Console.WriteLine("Updated metadata {0}", metadoc);

            //cleanup
            var sql = "select value c._self from c where c.id in('TrigTestMeta.1', '_metadata')";
            var documentsLinks = client.CreateDocumentQuery(collectionLink, sql).AsEnumerable();

            try
            {
                foreach (string item in documentsLinks)
                {
                    await client.DeleteDocumentAsync(item);
                }
            }
            catch (DocumentClientException ex)
            {
                Console.WriteLine("<script>alert('error : " + ex.Message + "');</script>");
            }
        }


        //Execute the pre-trigger...
        private async static Task Execute_trgEnsureUniqueId(DocumentClient client, Uri collectionLink)
        {
            // Creating a new document also creating triggers
            dynamic newDoc1 = new { id = "TrigTest.1", name = "Test Trigger" };
            dynamic newDoc2 = new { id = "TrigTest.1", name = "Test Trigger" };
            dynamic newDoc3 = new { id = "TrigTest.1", name = "Test Trigger" };

            var result1 = await client.CreateDocumentAsync(collectionLink, newDoc1, new RequestOptions { PreTriggerInclude = new[] { "trgEnsureUniqueId" } });

            var result2 = await client.CreateDocumentAsync(collectionLink, newDoc2, new RequestOptions { PreTriggerInclude = new[] { "trgEnsureUniqueId" } });

            var result3 = await client.CreateDocumentAsync(collectionLink, newDoc3, new RequestOptions { PreTriggerInclude = new[] { "trgEnsureUniqueId" } });

            //cleanup
            var sql = "select value c._self from c where startswith(c.id, 'TrigTest.1') = true";
            var documentsLinks = client.CreateDocumentQuery(collectionLink, sql).AsEnumerable();

            try
            {
                foreach (string item in documentsLinks)
                {
                    await client.DeleteDocumentAsync(item);
                }
            }
            catch (DocumentClientException ex)
            {
                Console.WriteLine("<script>alert('error : " + ex.Message + "');</script>");
            }
        }
    

    }
}
