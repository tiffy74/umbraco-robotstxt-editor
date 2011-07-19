﻿namespace Our.Umbraco.Tree.RobotsTxt
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using umbraco;
	using umbraco.BasePages;
	using umbraco.BusinessLogic;
	using umbraco.uicontrols;

	/// <summary>
	/// The Robots.txt Editor page class.
	/// </summary>
	public partial class EditRobotsTxtFile : UmbracoEnsuredPage
	{
		/// <summary>
		/// Saves the contents of the Robots.txt to disk.
		/// </summary>
		/// <param name="contents">The contents of the Robots.txt file.</param>
		/// <returns>Whether the save was successful.</returns>
		public bool SaveRobotsTxt(string contents)
		{
			try
			{
				string filePath = Server.MapPath("~/robots.txt");

				using (StreamWriter writer = File.CreateText(filePath))
				{
					writer.Write(contents);
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// A very basic Robots.txt validation.
		/// </summary>
		/// <param name="contents">The contents of the Robots.txt file.</param>
		/// <returns>A list of Pair objects, containing the line number and detail of the error.</returns>
		public List<Pair> ValidateRobotsTxt(string contents)
		{
			List<string> lines = new List<string>(contents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
			List<Pair> errors = new List<Pair>();

			for (int i = 0; i < lines.Count; i++)
			{
				string line = lines[i].Trim().ToUpper();

				// check if the line is invalid
				if ((!line.StartsWith("#")) && (!line.StartsWith("USER-AGENT")) && (!line.StartsWith("DISALLOW")) && (!line.StartsWith("ALLOW")) && (!line.StartsWith("SITEMAP")) && (!line.StartsWith("CRAWL-DELAY")) && (!line.StartsWith("REQUEST-RATE")) && (!line.StartsWith("VISIT-TIME")))
				{
					// invalid command
					errors.Add(new Pair(i, line));
				}
			}

			return errors;
		}

		/// <summary>
		/// Raises the <see cref="E:Init"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);

			if (this.UmbracoPanel1.hasMenu)
			{
				// add the save button
				ImageButton menuSave = this.UmbracoPanel1.Menu.NewImageButton();
				menuSave.AlternateText = "Save Robots.txt";
				menuSave.ImageUrl = String.Concat(GlobalSettings.Path, "/images/editor/save.gif");
				menuSave.Click += new ImageClickEventHandler(this.MenuSave_Click);

				this.UmbracoPanel1.Menu.InsertSplitter();

				// add a User-Agent button
				MenuIconI menuIcon = this.UmbracoPanel1.Menu.NewIcon();
				menuIcon.ImageURL = String.Concat(GlobalSettings.Path, "/robots-txt/user-agent.gif");
				menuIcon.OnClickCommand = "robotsTxtInsertRule('User-agent: ', '*');";
				menuIcon.AltText = "Insert User-Agent rule";

				// add a Disallow button
				menuIcon = this.UmbracoPanel1.Menu.NewIcon();
				menuIcon.ImageURL = String.Concat(GlobalSettings.Path, "/robots-txt/disallow.gif");
				menuIcon.OnClickCommand = "robotsTxtInsertRule('Disallow: ', '/');";
				menuIcon.AltText = "Insert Disallow rule";

				// add a Comment Out button
				menuIcon = this.UmbracoPanel1.Menu.NewIcon();
				menuIcon.ImageURL = String.Concat(GlobalSettings.Path, "/robots-txt/lines-comment.gif");
				menuIcon.OnClickCommand = "robotsTxtCommentOutRules();";
				menuIcon.AltText = "Comment out rules";

				// add an Uncomment button
				menuIcon = this.UmbracoPanel1.Menu.NewIcon();
				menuIcon.ImageURL = String.Concat(GlobalSettings.Path, "/robots-txt/lines-uncomment.gif");
				menuIcon.OnClickCommand = "robotsTxtUncommentRules();";
				menuIcon.AltText = "Uncomment rules";
			}
		}

		/// <summary>
		/// Handles the Load event of the Page control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected void Page_Load(object sender, EventArgs e)
		{
			string filePath = Server.MapPath("~/robots.txt");
			string fileContents = string.Empty;

			if (!IsPostBack)
			{
				// HACK: Brute-forces an Umbraco appTree install
				if (Request.QueryString["action"] == "install")
				{
					ApplicationTree appTree = ApplicationTree.getByAlias("robotsTxt");
					if (appTree == null)
					{
						ApplicationTree.MakeNew(false, true, 6, "developer", "robotsTxt", "Robots.txt", "../../robots-txt/robot.png", "../../robots-txt/robot.png", "Our.Umbraco.Tree.RobotsTxt", "RobotsTxtTree", String.Empty);
					}
				}

				// check if the robots.txt exists
				if (!File.Exists(filePath))
				{
					// if not, then use the embedded robots.txt
					Assembly assembly = Assembly.GetExecutingAssembly();
					if (assembly != null)
					{
						using (StreamReader robotsTxt = new StreamReader(assembly.GetManifestResourceStream("Our.Umbraco.Tree.RobotsTxt.robots.txt")))
						{
							fileContents = robotsTxt.ReadToEnd();
						}
					}
				}
				else
				{
					// otherwise read the contents
					using (StreamReader reader = File.OpenText(filePath))
					{
						fileContents = reader.ReadToEnd();
					}
				}

				if (!String.IsNullOrEmpty(fileContents))
				{
					this.editorSource.Text = fileContents;
				}
			}
		}

		/// <summary>
		/// Handles the Click event of the menuSave control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Web.UI.ImageClickEventArgs"/> instance containing the event data.</param>
		protected void MenuSave_Click(object sender, ImageClickEventArgs e)
		{
			List<Pair> errors = new List<Pair>();

			if (!this.SkipTesting.Checked)
			{
				errors = this.ValidateRobotsTxt(this.editorSource.Text);
			}

			if (errors.Count > 0)
			{
				StringBuilder sb = new StringBuilder(ui.Text("errors", "xsltErrorText").Replace("XSLT", "robots.txt"));
				string format = ui.Text("errorHandling", "errorRegExp", new string[] { "{0}", "line {1}" }, null);

				sb.AppendLine("<ul>");

				foreach (Pair error in errors)
				{
					sb.AppendLine("<li>").AppendFormat(format, error.Second, error.First).AppendLine("</li>");
				}

				sb.AppendLine("</ul>");

				// display the error message
				this.Feedback1.Text = sb.ToString();
				this.Feedback1.type = umbraco.uicontrols.Feedback.feedbacktype.error;
				this.Feedback1.Visible = true;
			}
			else
			{
				// save the file if there are no errors
				if (this.SaveRobotsTxt(this.editorSource.Text))
				{
					ClientTools.ShowSpeechBubble(BasePage.speechBubbleIcon.save, ui.Text("speechBubbles", "fileSavedHeader"), ui.Text("speechBubbles", "fileSavedText"));
				}
				else
				{
					ClientTools.ShowSpeechBubble(BasePage.speechBubbleIcon.error, ui.Text("speechBubbles", "fileErrorHeader"), ui.Text("speechBubbles", "fileErrorText"));
				}
			}
		}
	}
}