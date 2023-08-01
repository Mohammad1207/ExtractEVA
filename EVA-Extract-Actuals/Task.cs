using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{
	class Tasks : TaskObject
	{
		public override decimal PlannedCost { get; set; }

		public override decimal Progress { get; set; }

		public override int Period { get; set; }

		public override decimal ActualCost { get; set; }

	}
}
