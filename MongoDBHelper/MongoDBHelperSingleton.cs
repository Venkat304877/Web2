using System;

namespace WebApplication.MongoDBHelper
{
    public class MongoDBHelpersingleton
    {
        public static MongoDBHelpersingleton instance = null;
        private MongoClient client;
        private string password;
        private string userName;
        private string host;
        private string dbName;

        private MongoDBHelpersingleton()
        {
           AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider(); 
            KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)); 
            password = keyVaultClient.GetSecretAsync("https://<keyvaulturi>/secrets/DBPassword").Result.Value; 
            userName = keyVaultClient.GetSecretAsync("https://<keyvaulturi>/secrets/DBUserName").Result.Value; 
            host = keyVaultClient.GetSecretAsync("https://<keyvaulturi>/secrets/DBHost").Result.Value; 
            dbName = keyVaultClient.GetSecretAsync("https://<keyvaulturi>/secrets/DBName").Result.Value; 

            MongoClientSettings settings = new MongoClientSettings(); 
            settings.Server = new MongoServerAddress(host, 10255); 
            settings.UseTls = true; 
            settings.SslSettings = new SslSettings(); 
            settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12; 
            settings.RetryWrites = false; 
            settings.RetryReads = false; 

 

            MongoIdentity identity = new MongoInternalIdentity(dbName, userName); 
            MongoIdentityEvidence evidence = new PasswordEvidence(password); 
 
            settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence); 

            client = new MongoClient(settings); 
        }

        public static void InitializeSingleton() 
        { 
            if(instance == null) 
                instance = new MongoDBHelperSingleton(); 
        } 

        //We will want to get the specified collection of where our record is stored while querying the DB
        private IMongoCollection<BsonDocument> GetCollection(string collectionName) 
        { 
            var database = client.GetDatabase(dbName); 
            var todoTaskCollection = database.GetCollection<BsonDocument>(collectionName); 
            return todoTaskCollection; 
        } 

        //This method will allow us to query our DB with a set of filters at the input.
        public List<BsonDocument> GetRecords(string collectionName, FilterDefinition<BsonDocument>[] filters) 
        { 
            var collection = GetCollection(collectionName); 
            var filter = Builders<BsonDocument>.Filter.And(filters); 
 
            return collection.Find(filter).ToList<BsonDocument>(); 
        } 
         
         //This method will create a new record in the DB.
        public bool CreateRecord(BsonDocument record, string collectionName) 
        { 
            var collection = GetCollection(collectionName); 
            FilterDefinition<BsonDocument>[] filters = new FilterDefinition<BsonDocument>[] 
                    { Builders<BsonDocument>.Filter.Eq("_id", record.GetValue("_id").AsString) }; 
            List<BsonDocument> result = GetRecords(collectionName, filters); 

            if (result.Count > 0) 
            { 
                return true; 
            } 
            collection.InsertOne(record); 
            return false; 
        } 

        //This method will update a record in the DB. We can set the new value and call the record to update it. 
        public BsonDocument GetRecordAndUpdate(string id, Dictionary<string, object> fieldsToUpdate, string collectionName) 
        { 
            var collection = GetCollection(collectionName); 
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id); 

            List<UpdateDefinition<BsonDocument>> updates = new List<UpdateDefinition<BsonDocument>>(); 

            foreach (KeyValuePair<string, object> pair in fieldsToUpdate) 
            { 
                var update = Builders<BsonDocument>.Update.Set(pair.Key, pair.Value); 
                updates.Add(update); 
            } 

            var allUpdates = Builders<BsonDocument>.Update.Combine(updates); 
            BsonDocument result = collection.FindOneAndUpdate(filter, allUpdates); 

            return result; 
        } 

    }
}