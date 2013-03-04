using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Web.UI.HtmlControls;

namespace Website.Aion
{
	public partial class Main : MasterPage
	{
		protected override void OnPreRender (EventArgs e)
		{
			Utils.SetCompressFilter ();

			this.Page.ClientScript.RegisterClientScriptBlock (this.GetType (), "reset_page_load", "var IsPageLoaded = false;", true);
			this.Page.ClientScript.RegisterStartupScript (this.GetType (), "set_page_load", "IsPageLoaded = true;", true);
			this.Page.ClientScript.RegisterOnSubmitStatement (this.GetType (), "global_submit", "if(!IsPageLoaded){alert('Пожалуйста, дождитесь завершения загрузки страницы.');return false;}");

			this.strOldBrowserWarning.Text = Resources.MasterPage.OldBrowserWarning;
			this.strNoJavaScriptWarning.Text = Resources.MasterPage.NoJavaScriptWarning;

			this.panelOldBrowser.Visible = Utils.IsOldBrowser ();
			string v = Utils.GetCurrentVersion (true);

			this.headIncludes.Text =
				"<link href='StaticCss.axd?v=" + v + "' type='text/css' rel='stylesheet' />" +
				"<script type='text/javascript' src='StaticJs.axd?v=" + v + "'></script>" +
				(this.Page.Items["AdditionalHeaderIncludes"] ?? string.Empty);

			// set meta "description"
			string meta_description = this.Page.Items["MetaDescription"] as string;
			if (!string.IsNullOrEmpty (meta_description))
			{
				HtmlMeta meta = new HtmlMeta ();
				meta.Name = "description";
				meta.Content = meta_description;

				this.Page.Header.Controls.Add (meta);
			}

			// set meta "keywords"
			string meta_keywords = this.Page.Items["MetaKeywords"] as string;
			if (!string.IsNullOrEmpty (meta_keywords))
			{
				HtmlMeta meta = new HtmlMeta ();
				meta.Name = "keywords";
				meta.Content = meta_keywords;

				this.Page.Header.Controls.Add (meta);
			}

			base.OnPreRender (e);
		}
	}
}
