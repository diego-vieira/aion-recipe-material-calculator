using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Configuration;

namespace Website
{
	public class Global : HttpApplication
	{
		#region Cache methods

		/// <summary>
		/// Handles <see cref="CacheManager.ClearCache"/> event.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event arguments.</param>
		private void CacheManager_ClearCache (object sender, EventArgs e)
		{
			try
			{
				int value = int.Parse ((string) this.Application["VaryByCustomString.ClearCache"], NumberStyles.Integer, CultureInfo.InvariantCulture);
				value++;

				this.Application["VaryByCustomString.ClearCache"] = value.ToString (CultureInfo.InvariantCulture);

				//SiteMapTree.ClearCache ();
			}
			catch
			{
				this.Application["VaryByCustomString.ClearCache"] = "0";
			}
		}

		/// <summary>
		/// GetVaryByCustomString
		/// </summary>
		/// <param name="context"></param>
		/// <param name="custom"></param>
		/// <returns></returns>
		public override string GetVaryByCustomString (HttpContext context, string custom)
		{
			try
			{
				StringBuilder res = new StringBuilder ();
				if (!String.IsNullOrEmpty (custom))
				{
					string[] keys = custom.Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string key in keys)
					{
						if (key == "ClearCache")
							res.Append ((string) context.Application["VaryByCustomString.ClearCache"]);

						res.Append (';');
					}
				}

				res.Append (base.GetVaryByCustomString (context, custom));

				return res.ToString ();
			}
			catch
			{
				return base.GetVaryByCustomString (context, custom);
			}
		}

		#endregion

		protected void Application_Start (object sender, EventArgs e)
		{
			try
			{
				Regex.CacheSize = 255;

				// cache
				this.Application["VaryByCustomString.ClearCache"] = "0";
				CacheManager.ClearCache += new EventHandler (this.CacheManager_ClearCache);
				CacheManager.IsEnabled = bool.Parse (ConfigurationManager.AppSettings["CacheManager.IsEnabled"]);
			}
			catch
			{
			}
		}

		//protected void Application_BeginRequest (object sender, EventArgs e)
		//{
		//}

		//protected void Application_EndRequest (object sender, EventArgs e)
		//{
		//}

		//protected void Session_Start (object sender, EventArgs e)
		//{
		//}

		//protected void Application_AuthenticateRequest (object sender, EventArgs e)
		//{
		//}

		//protected void Application_Error (object sender, EventArgs e)
		//{
		//}

		//protected void Session_End (object sender, EventArgs e)
		//{
		//}

		//protected void Application_End (object sender, EventArgs e)
		//{
		//}
	}
}