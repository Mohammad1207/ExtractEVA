using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace EVA_Extract_Actuals
{
    public class CustomDistribution
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Owner { get; set; }

        public string Distribution { get; set; }

    }

    public class EVADistributionSaveModel
    {

        public string Id { get; set; }

        public string Name { get; set; }

        public string Distribution { get; set; }

    }

}
