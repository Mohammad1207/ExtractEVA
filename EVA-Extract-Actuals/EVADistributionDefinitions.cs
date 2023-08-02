using EVA_Extract_Actuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{
    public class Distribution
    {

        public string Id { get; set; }

        public string Name { get; set; }

        public TaskPackage RootTaskPackage { get; set; }
    }


    public class ExpectedProgressEntry
    {

        public string Id { get; set; }

        public DateOnly? CreatedTime { get; set; }

        public string Name { get; set; }

        public string Owner { get; set; }

        public string Project { get; set; }

    }

    public class DistributionType
    {
        public string Code { get; set; }

        public string Name { get; set; }
    }

}
