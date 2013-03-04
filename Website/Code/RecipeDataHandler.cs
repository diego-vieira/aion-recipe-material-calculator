using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Threading;

namespace Website.Aion
{
	public class RecipeDataHandler : BaseHandler
	{
		protected override void Initialize ()
		{
			base.Initialize ();

			this.CacheVaryByParams.Add ("rid");
			this.CacheVaryByParams.Add ("id");
			this.CacheVaryByParams.Add ("l");
			this.CacheVaryByParams.Add ("v");
		}

		protected override bool Process (DateTime? lastModified, string etag)
		{
			int rid = Utils.ConvertString (this.Context.Request.QueryString["rid"], 0);
			int id = Utils.ConvertString (this.Context.Request.QueryString["id"], 0);


			List<LanguageInfo> languages = Utils.GetLanguages ("Aion.Languages");
			LanguageInfo lang = languages.SingleOrDefault (x => x.Name == (this.Context.Request.QueryString["l"] ?? string.Empty).ToLowerInvariant ());

			if (lang == null)
				throw new InvalidOperationException ("Invalid arguments");

            if (!Directory.Exists(this.Context.Server.MapPath("~/Recipes/" + lang.Name)))
                lang.Name = "en";

			string path = Path.Combine (this.Context.Server.MapPath ("~/Recipes/" + lang.Name), rid + "_" + id + ".js");
			this.LastModified = File.GetLastWriteTime (path);

			if ((lastModified.HasValue && lastModified.Value == this.LastModified.Value)
				||
				etag == this.GetETag (this.LastModified.Value))
				return false;

			if (!File.Exists (path))
			{
				this.Set404 ();
				return true;
			}

			Utils.SetCompressFilter ();
			this.Context.Response.Write (File.ReadAllText (path));
			this.Context.Response.ContentType = "text/plain";

			return true;
		}
	}
}
