// Decompiled with JetBrains decompiler
// Type: Ultrasound.fTools
// Assembly: Ultrasound, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 0A0C4A24-A00F-49E4-B8F5-373D63492914
// Assembly location: C:\Users\aliosatos\Desktop\UltraSound\Ultrasound.exe

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Voices;

namespace Ultrasound
{
	public class fTools : Form
	{
		public Button bAnalyse;
		public Button bExtract;
		public Button bFLevel;
		public Button bOutput;
		public IContainer components = null;
		public Label label1;
		public Label label2;
		public Label label3;
		public Label label4;
		public ListBox lbAResults;
		public ProgressBar PB;
		public FolderBrowserDialog selFolder;
		public TabControl tabControl1;
		public TabPage tabPage1;
		public TabPage tabPage2;
		public TextBox txtFLevel;
		public TextBox txtOutput;

		public fTools()
		{
			InitializeComponent();
		}

		public void bFLevel_Click(object sender, EventArgs e)
		{
			if (selFolder.ShowDialog() != DialogResult.OK)
				return;
			txtFLevel.Text = selFolder.SelectedPath;
		}

		public void bOutput_Click(object sender, EventArgs e)
		{
			if (selFolder.ShowDialog() != DialogResult.OK)
				return;
			txtOutput.Text = selFolder.SelectedPath;
		}

		public void SetXML(VoiceList vl, string file)
		{
			VoiceList t1;
			if (File.Exists(file))
				using (var fileStream = new FileStream(file, FileMode.Open))
				{
					t1 = Util.Deserialise<VoiceList>(fileStream);
				}
			else
				t1 = new VoiceList
				{
					Entries = new List<VoiceEntry>()
				};

			var t2 = new DummyEntries
			{
				Entries = new List<VoiceEntry>()
			};
			foreach (var entry in vl.Entries)
			{
				var e = entry;
				var voiceEntry = t1.Entries.Find(ve =>
					ve.DID == e.DID && (ve.SID ?? string.Empty).Equals(e.SID ?? string.Empty));
				if (voiceEntry != null)
					voiceEntry.Dialogue = e.Dialogue;
				else if (e.Dialogue.Contains('“'))
					t1.Entries.Add(e);
				else
					t2.Entries.Add(e);
			}

			using (var fileStream = new FileStream(file, FileMode.Create))
			{
				Util.Serialise(t1, fileStream);
			}

			if (!t2.Entries.Any())
				return;
			var xmlDocument1 = new XmlDocument();
			xmlDocument1.Load(file);
			var memoryStream = new MemoryStream();
			Util.Serialise(t2, memoryStream);
			memoryStream.Position = 0L;
			var xmlDocument2 = new XmlDocument();
			xmlDocument2.Load(memoryStream);
			var data =
				"The following entries don't look like dialogue. You can uncomment them if they are needed.\n\n\t" +
				xmlDocument2.SelectSingleNode("/DummyEntries").InnerXml.Replace("--", "~~")
					.Replace("<Entry", "\n\t<Entry") + "\n";
			var comment = xmlDocument1.CreateComment(data);
			xmlDocument1.SelectSingleNode("/VoiceList").AppendChild(comment);
			xmlDocument1.Save(file);
		}

		public void DoExtract(string input, string output)
		{
			var t = new VoiceIndex
			{
				Entries = new List<IndexEntry>()
			};
			using (var fileStream1 = new FileStream(Path.Combine(input, "maplist"), FileMode.Open))
			{
				var num1 = Util.ReadUShortFrom(fileStream1, 0);
				var numArray = new byte[32];
				foreach (var num2 in Enumerable.Range(0, num1))
				{
					fileStream1.Position = 2 + 32 * num2;
					fileStream1.Read(numArray, 0, 32);
					var str = Encoding.ASCII.GetString(numArray).Trim().TrimEnd(new char[1]);
					var path = Path.Combine(input, str);
					if (File.Exists(path))
					{
						using (var fileStream2 = new FileStream(path, FileMode.Open))
						{
							var vl = Dumper.Dump(fileStream2, str);
							if (vl != null)
							{
								var file = Path.Combine(output, str + ".xml");
								SetXML(vl, file);
							}
							else
							{
								continue;
							}
						}

						t.Entries.Add(new IndexEntry
						{
							File = str + ".xml",
							FieldID = num2
						});
					}

					Debug.WriteLine("Processed " + str);
				}
			}

			using (var fileStream = new FileStream(Path.Combine(output, "index.xml"), FileMode.Create))
			{
				Util.Serialise(t, fileStream);
			}
		}

		public void bExtract_Click(object sender, EventArgs e)
		{
			DoExtract(txtFLevel.Text, txtOutput.Text);
		}

		public void bAnalyse_Click(object sender, EventArgs e)
		{
			var backgroundWorker = new BackgroundWorker
			{
				WorkerReportsProgress = true
			};
			backgroundWorker.DoWork += bw_DoWork;
			backgroundWorker.ProgressChanged += bw_ProgressChanged;
			backgroundWorker.RunWorkerCompleted += bw_RunWorkerCompleted;
			backgroundWorker.RunWorkerAsync();
		}

		public void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			lbAResults.Items.Clear();
			lbAResults.Items.AddRange(((List<string>) e.Result).ToArray());
		}

		public void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			PB.Value = e.ProgressPercentage;
		}

		public void bw_DoWork(object sender, DoWorkEventArgs e)
		{
			var stringList = new List<string>();
			var data = ConfigurationManager.AppSettings["DataFolder"];
			VoiceIndex voiceIndex;
			using (var fileStream = new FileStream(Path.Combine(data, "index.xml"), FileMode.Open))
			{
				voiceIndex = Util.Deserialise<VoiceIndex>(fileStream);
			}

			var num1 = 0;
			var num2 = 0;
			var num3 = 0;
			foreach (var entry in voiceIndex.Entries)
			{
				var path = Path.Combine(data, entry.File);
				if (File.Exists(path))
				{
					VoiceList voiceList;
					using (var fileStream = new FileStream(path, FileMode.Open))
					{
						voiceList = Util.Deserialise<VoiceList>(fileStream);
					}

					if (voiceList.Entries.Count == 0)
					{
						stringList.Add(string.Format("File {0} - no entries", entry.File));
						continue;
					}

					var num4 = voiceList.Entries.Where(ve => File.Exists(Path.Combine(data, ve.File))).Count();
					num2 += voiceList.Entries.Count;
					num3 += num4;
					stringList.Add(string.Format("File {0} - {1}/{2} done ({3}%)", (object) entry.File, (object) num4,
						(object) voiceList.Entries.Count, (object) (100 * num4 / voiceList.Entries.Count)));
				}
				else
				{
					stringList.Add("ERROR - " + path + " not found");
				}

				(sender as BackgroundWorker).ReportProgress(100 * num1 / voiceIndex.Entries.Count);
			}

			stringList.Insert(0, "");
			stringList.Insert(0, string.Format("OVERALL: {0}/{1} done ({2}%)", num3, num2, 100 * num3 / num2));
			e.Result = stringList;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
				components.Dispose();
			base.Dispose(disposing);
        }

        public void InitializeComponent()
        {
			tabControl1 = new TabControl();
			tabPage1 = new TabPage();
			tabPage2 = new TabPage();
			bFLevel = new Button();
			txtFLevel = new TextBox();
			selFolder = new FolderBrowserDialog();
			label1 = new Label();
			label2 = new Label();
			label3 = new Label();
			txtOutput = new TextBox();
			bOutput = new Button();
			label4 = new Label();
			PB = new ProgressBar();
			bExtract = new Button();
			lbAResults = new ListBox();
			bAnalyse = new Button();
			tabControl1.SuspendLayout();
			tabPage1.SuspendLayout();
			tabPage2.SuspendLayout();
			SuspendLayout();
			tabControl1.Controls.Add(tabPage2);
			tabControl1.Controls.Add(tabPage1);
			tabControl1.Dock = DockStyle.Fill;
			tabControl1.Location = new Point(0, 0);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new Size(1002, 556);
			tabControl1.TabIndex = 0;
			tabPage1.Controls.Add(bExtract);
			tabPage1.Controls.Add(label4);
			tabPage1.Controls.Add(label3);
			tabPage1.Controls.Add(txtOutput);
			tabPage1.Controls.Add(bOutput);
			tabPage1.Controls.Add(label2);
			tabPage1.Controls.Add(label1);
			tabPage1.Controls.Add(txtFLevel);
			tabPage1.Controls.Add(bFLevel);
			tabPage1.Location = new Point(4, 34);
			tabPage1.Name = "tabPage1";
			tabPage1.Padding = new Padding(3);
			tabPage1.Size = new Size(994, 518);
			tabPage1.TabIndex = 0;
			tabPage1.Text = "Extract Voice files";
			tabPage1.UseVisualStyleBackColor = true;
			tabPage2.Controls.Add(bAnalyse);
			tabPage2.Controls.Add(lbAResults);
			tabPage2.Location = new Point(4, 34);
			tabPage2.Name = "tabPage2";
			tabPage2.Padding = new Padding(3);
			tabPage2.Size = new Size(994, 518);
			tabPage2.TabIndex = 1;
			tabPage2.Text = "Analyse";
			tabPage2.UseVisualStyleBackColor = true;
			bFLevel.Location = new Point(674, 23);
			bFLevel.Name = "bFLevel";
			bFLevel.Size = new Size(65, 43);
			bFLevel.TabIndex = 0;
			bFLevel.Text = "...";
			bFLevel.UseVisualStyleBackColor = true;
			bFLevel.Click += bFLevel_Click;
			txtFLevel.AutoCompleteMode = AutoCompleteMode.Suggest;
			txtFLevel.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
			txtFLevel.Location = new Point(225, 29);
			txtFLevel.Name = "txtFLevel";
			txtFLevel.Size = new Size(434, 31);
			txtFLevel.TabIndex = 1;
			label1.AutoSize = true;
			label1.Location = new Point(24, 32);
			label1.Name = "label1";
			label1.Size = new Size(195, 25);
			label1.TabIndex = 2;
			label1.Text = "Input FLEVEL files:";
			label2.AutoSize = true;
			label2.Location = new Point(220, 90);
			label2.Name = "label2";
			label2.Size = new Size(609, 25);
			label2.TabIndex = 3;
			label2.Text = "Extract and decompress flevel.lgp to a folder and select it here";
			label3.AutoSize = true;
			label3.Location = new Point(34, 171);
			label3.Name = "label3";
			label3.Size = new Size(184, 25);
			label3.TabIndex = 6;
			label3.Text = "Output voice files:";
			txtOutput.AutoCompleteMode = AutoCompleteMode.Suggest;
			txtOutput.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
			txtOutput.Location = new Point(225, 168);
			txtOutput.Name = "txtOutput";
			txtOutput.Size = new Size(434, 31);
			txtOutput.TabIndex = 5;
			bOutput.Location = new Point(674, 163);
			bOutput.Name = "bOutput";
			bOutput.Size = new Size(65, 41);
			bOutput.TabIndex = 4;
			bOutput.Text = "...";
			bOutput.UseVisualStyleBackColor = true;
			bOutput.Click += bOutput_Click;
			label4.AutoSize = true;
			label4.Location = new Point(220, 241);
			label4.Name = "label4";
			label4.Size = new Size(342, 25);
			label4.TabIndex = 7;
			label4.Text = "Folder to save voice XML files into";
			PB.Dock = DockStyle.Bottom;
			PB.Location = new Point(0, 556);
			PB.Name = "PB";
			PB.Size = new Size(1002, 23);
			PB.TabIndex = 1;
			bExtract.Location = new Point(413, 425);
			bExtract.Name = "bExtract";
			bExtract.Size = new Size(128, 46);
			bExtract.TabIndex = 10;
			bExtract.Text = "Extract";
			bExtract.UseVisualStyleBackColor = true;
			bExtract.Click += bExtract_Click;
			lbAResults.FormattingEnabled = true;
			lbAResults.ItemHeight = 25;
			lbAResults.Location = new Point(19, 27);
			lbAResults.Name = "lbAResults";
			lbAResults.Size = new Size(948, 379);
			lbAResults.TabIndex = 0;
			bAnalyse.Location = new Point(393, 433);
			bAnalyse.Name = "bAnalyse";
			bAnalyse.Size = new Size(183, 53);
			bAnalyse.TabIndex = 1;
			bAnalyse.Text = "Analyse";
			bAnalyse.UseVisualStyleBackColor = true;
			bAnalyse.Click += bAnalyse_Click;
			AutoScaleDimensions = new SizeF(12f, 25f);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1002, 579);
			Controls.Add(tabControl1);
			Controls.Add(PB);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			Text = "Voice Tools";
			tabControl1.ResumeLayout(false);
			tabPage1.ResumeLayout(false);
			tabPage1.PerformLayout();
			tabPage2.ResumeLayout(false);
			ResumeLayout(false);
		}

		public class DummyEntries
		{
			[XmlElement("Entry")]
			public List<VoiceEntry> Entries { get; set; }
		}
	}
}