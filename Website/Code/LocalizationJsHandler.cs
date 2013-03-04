using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Resources;
using Resources;
using System.Globalization;
using System.Collections;

namespace Website.Aion
{
	/// <summary>
	/// <para>Code generated localization javascript file handler.</para>
	/// <para><b>Author:</b> Logutov Michael<br />
	/// <b>Creation date:</b> 25 november 2009</para>
	/// </summary>
	public class LocalizationJsHandler : BaseHandler
	{
		protected override void Initialize ()
		{
			base.Initialize ();

			this.CacheVaryByParams.Add ("l");
		}

		protected override bool Process (DateTime? lastModified, string etag)
		{
			this.LastModified = File.GetLastWriteTime (this.GetType ().Assembly.Location);

			if ((lastModified.HasValue && lastModified.Value == this.LastModified.Value)
				||
				etag == this.GetETag (this.LastModified.Value))
				return false;

			List<string> js = new List<string> ();

			// localization
			{
				List<LanguageInfo> languages = Utils.GetLanguages ("Aion.Languages");
				LanguageInfo lang = languages.SingleOrDefault (x => x.Name == (this.Context.Request.QueryString["l"] ?? string.Empty).ToLowerInvariant ());

				if (lang == null)
					throw new InvalidOperationException ("Invalid arguments");

				CultureInfo culture = CultureInfo.GetCultureInfo (lang.Culture);
				CultureInfo culture_en = CultureInfo.GetCultureInfo ("en-US");

				ResourceSet rs = AionRecipeCalculator.ResourceManager.GetResourceSet (culture, true, true);
				ResourceSet rs_en = AionRecipeCalculator.ResourceManager.GetResourceSet (culture_en, true, true);
				if (rs != null && rs_en != null)
				{
					IDictionaryEnumerator e = rs_en.GetEnumerator ();
					while (e.MoveNext ())
					{
						if (e.Value == null)
							continue;

						string key = e.Key.ToString ();
						string value = rs.GetString (key);
						if (string.IsNullOrEmpty (value))
							value = e.Value.ToString ();

						js.Add ("'" + key + "':'" + value.Replace ("'", "\\'") + "'");
					}
				}
			}

			Utils.SetCompressFilter ();
			this.Context.Response.Write ("var Localization = {" + string.Join (",", js.ToArray ()) + "};");
			this.Context.Response.ContentType = "text/javascript";

			return true;
		}
	}
}
