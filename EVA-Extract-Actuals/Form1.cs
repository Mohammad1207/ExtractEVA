using Amazon.Auth.AccessControlPolicy;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace EVA_Extract_Actuals
{
	public partial class Form1 : Form
	{
		private string sourceFileName = "";

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				this.sourceFileName = openFileDialog1.FileName;
				saveFileDialog1.InitialDirectory = Path.GetFullPath(sourceFileName);

			}

		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				var saveFileName = saveFileDialog1.FileName;
				try
				{
					var taskRows = new List<TaskObject>();
					var fileInfo1 = new FileInfo(sourceFileName);
					XmlDocument source = new XmlDocument();
					using (var inStream = fileInfo1.OpenRead())
					{
						source.Load(inStream);
					}

					var taskRoot = new TaskCategory();
					taskRoot.Code = string.Empty;

					var child = source.ChildNodes;
					XmlNodeList taskRootNode = source.SelectNodes("/ProjectDocument/Project/Children[1]/Child");
                    //XmlNode taskRootNode = source.SelectSingleNode("/ProjectDocument/Project/Children[1]/Child");

                    var database = Database.GetDatabase();
                    var taskCollection = database.GetCollection<EVAProject>("EVA");
					
                    var project = BuildDB.GenerateProject(source, taskRootNode[0], taskRootNode);
                    project.CurrentHash = BuildCurrentHash(project);
                    taskCollection.InsertOne(project);

                    ProcessNodeList(taskRoot, taskRootNode);
					AssignCodes(taskRoot);
					Flatten(taskRoot, taskRows);
					using (StreamWriter file = new StreamWriter(saveFileName))
					{
						file.WriteLine("Code,Name,Period,Progress,Cost");
						foreach (var taskRow in taskRows)
						{
							file.WriteLine(taskRow.OutLine);
						}
					}
					return;
				}
				catch (SecurityException ex)
				{
					MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
					$"Details:\n\n{ex.StackTrace}");
				}
			}
		}

		private void Flatten(TaskCategory root, List<TaskObject> flatList)
		{
			foreach (TaskObject child in root.Children)
			{
				flatList.Add(child);
				if (child is TaskCategory cat)
				{
					Flatten(cat, flatList);
				}
			}
		}

		private void AssignCodes(TaskCategory root)
		{
			int index = 1;
			foreach (TaskObject child in root.Children)
			{
				if (root.Code != string.Empty)
				{
					child.Code = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", root.Code, index);
				}
				else
				{
					child.Code = string.Format(CultureInfo.CurrentCulture, "{0}", index);
				}
				index++;
				if (child is TaskCategory childCat)
				{
					AssignCodes(childCat);
				}
			}
			
		}

        private void ProcessNodeList(TaskCategory objectParent, XmlNodeList children)
        {
            foreach (XmlElement child in children)
            {
                var objectTypeString = child.GetAttribute("AssemblyQualifiedName");
                var actuals = child.SelectNodes("Actuals/Actual[last()]");
                var period = 0;
                var progress = 0M;
                var cost = 0M;
                if (actuals.Count > 0)
                {
                    var actualData = (XmlElement)actuals.Item(0);
                    progress = decimal.Parse(actualData.GetAttribute("PercentComplete"));
                    progress = decimal.Round(progress * 100, 1);
                    period = int.Parse(actualData.GetAttribute("Period"));
                    cost = decimal.Parse(actualData.GetAttribute("ActualCost"));
                    cost = decimal.Round(cost, 2);

                }
                if (objectTypeString.Contains("WorkPackage"))
                {

                    TaskCategory newCat = new TaskCategory();
                    newCat.CodeOverride = child.GetAttribute("CodeOverride");
                    newCat.Name = child.GetAttribute("Name");
                    newCat.ActualCost = cost;
                    newCat.Period = period;
                    newCat.Progress = progress;
                    objectParent.Children.Add(newCat);
                    XmlNodeList packageChildren = child.SelectNodes("Children/Child");
                    ProcessNodeList(newCat, packageChildren);
                }
                else if (objectTypeString.Contains("WorkTask"))
                {
                    Tasks newTask = new Tasks();
                    var plannedCost = decimal.Parse(child.GetAttribute("PlannedCost"));
                    plannedCost = decimal.Round(plannedCost, 2);
                    newTask.CodeOverride = child.GetAttribute("CodeOverride");
                    newTask.Name = child.GetAttribute("Name");
                    newTask.ActualCost = cost;
                    newTask.Period = period;
                    newTask.Progress = progress;
                    newTask.PlannedCost = plannedCost;
                    objectParent.Children.Add(newTask);
                }
            }
        }

        public static string BuildCurrentHash(EVAProject project)
        {
            var hash = "";
            var dataString = project.OwnerName + project.Name + project.FolderName + project.IsArchive;
            var fullmd5 = MD5.Create().ComputeHash(UTF8Encoding.ASCII.GetBytes((project.RootTaskPackage.BuildMD5().ToString() ?? "") + dataString));
            foreach (var fullmd in fullmd5)
            {
                hash += fullmd.ToString("X2");
            }
            return hash;
        }

    }
}
