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

                try
                {
                    var fileInfo1 = new FileInfo(sourceFileName);
                    XmlDocument source = new();
                    using (var inStream = fileInfo1.OpenRead())
                    {
                        source.Load(inStream);
                    }

                    var child = source.ChildNodes;
                    var projectName = fileInfo1.Name;
                    XmlNodeList taskRootNode = source.SelectNodes("/ProjectDocument/Project/Children[1]/Child");

                    var database = Database.GetDatabase();
                    var taskCollection = database.GetCollection<EVAProject>("EVA");

                    var project = BuildDB.GenerateProject(projectName, source);
                    project.CurrentHash = BuildCurrentHash(project);
                    taskCollection.InsertOne(project);

                    return;
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }

		}

		/*private void button2_Click(object sender, EventArgs e)
		{

		}*/

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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
