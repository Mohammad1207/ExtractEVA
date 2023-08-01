using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Threading.Tasks;


namespace EVA_Extract_Actuals
{
    public class PreBaselineTask : PreBaselineTaskBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public override string Id { get; set; }
        internal override void ReDefinePreBaselineTaskID(List<TaskBase> dftProj)
        {
            var projTask = dftProj.Find(p => p.Code == this.Code);
            this.SourceId = projTask.Id;
            this.Parent = projTask.Parent;
        }

    }
}
