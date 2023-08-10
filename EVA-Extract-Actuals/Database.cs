using MongoDB.Driver;

namespace EVA_Extract_Actuals
{
    public class Database
    {
        public static class Collections
        {
            public static readonly string DISTRIBUTIONS = "DISTRIBUTIONS";

            public static readonly string USERS = "USERS";

            public static readonly string TASKS = "`TASKS";

            public static readonly string PRE_BASELINES = "PRE-BASELINE";

            public static readonly string PRE_BASELINE_TASKS = "PRE-BASELINE-TASKS";

        }

        public static IMongoDatabase GetDatabase()
        {
            MongoClient dbClient = new MongoClient("mongodb://eva-projects:YTiT8NNRNb8IrN6uhc1dlX3zlqiUc7TafHFez18b0VP7VVKWfAWqBHUc88lGKiWiCGs2zsdfLUw6VLdNArlcxQ==@eva-projects.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@eva-projects@");

            return dbClient.GetDatabase("EVA_NEW_DATA");
        }

    }
}
