using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace Website.Aion
{
	public class UploadNewDataHandler : BaseHandler
	{
		protected override void Initialize ()
		{
			base.Initialize ();
			this.NoCache = true;
		}

		protected override bool Process (DateTime? lastModified, string etag)
		{
			if (this.Context.Request.Files.Count < 1)
				throw new InvalidOperationException ("No files to upload");

			HttpPostedFile file = this.Context.Request.Files[0];
			string path = Path.Combine (this.Context.Server.MapPath ("~/Recipes"), "data.zip");

			if (File.Exists (path))
				File.Delete (path);

			file.SaveAs (path);
			return true;
		}
	}
}
