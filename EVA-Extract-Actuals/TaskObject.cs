using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{
	abstract class TaskObject
	{

		public string Code { get; set; }

		public string CodeOverride { get; set; }

		public abstract decimal ActualCost { get; set; }

		public abstract decimal PlannedCost { get; set; }

		public abstract int Period { get; set; }

		public abstract decimal Progress { get; set; }

		public string Name { get; set; }

		public string OutLine
		{
			get
			{
				var code = string.IsNullOrEmpty(CodeOverride) ? Code : CodeOverride;
				var quote = "\"";
				var result = quote + code + quote + "," + quote + Name + quote + "," + quote + Period + quote + "," + quote + Progress + quote + "," + quote + ActualCost + quote;
					return result;
			}
		}
	}
}
