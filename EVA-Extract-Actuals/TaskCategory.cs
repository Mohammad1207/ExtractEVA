using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{
	class TaskCategory : TaskObject
	{

		public List<TaskObject> Children { get; set; } = new List<TaskObject>();

		public override decimal PlannedCost
		{
			get
			{
				return Children.Select(_ => _.PlannedCost).Sum();
			}
			set
			{
				
			}
			
		}

		public override decimal ActualCost
		{
			get
			{
				return Children.Select(_ => _.ActualCost).Sum();
			}
			set
			{

			}

		}

		public override decimal Progress
		{
			get
			{
				if (PlannedCost == 0M)
				{
					return 0;
				}
				var actualProgress = 0M;
				foreach (TaskObject child in Children)
				{
					actualProgress += child.PlannedCost * (child.Progress / 100);
				}
				return decimal.Round((actualProgress / PlannedCost) * 100, 1);
			}
			set
			{

			}

		}

		public override int Period
		{
			get
			{
				return Children.Select(_ => _.Period).DefaultIfEmpty(0).Max();
			}
			set
			{

			}

		}
	}
}
