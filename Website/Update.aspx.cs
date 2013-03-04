using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Ionic.Zip;
using System.IO;

namespace Website
{
    public partial class Update : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] checkNewZips = Directory.GetFiles(Context.Server.MapPath(@"~/Recipes/"), "*.zip");
            foreach (var file in checkNewZips)
            {
                using (ZipFile zip = ZipFile.Read(file))
                {
                    foreach (ZipEntry f in zip)
                    {
                        f.Extract(Context.Server.MapPath(@"~/Recipes/"), ExtractExistingFileAction.OverwriteSilently);  // true => overwrite existing files
                    }
                }
            }

            Response.Write("All done");
        }
    }
}