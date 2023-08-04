using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{

    public class Task : TaskBase
    {
        public DistributionType Distribution { get; set; } = new DistributionType();
    }
}
