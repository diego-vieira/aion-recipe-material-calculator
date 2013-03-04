using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.ComponentModel;
using System.Xml.Xsl;
using System.Xml.XPath;

namespace Website.Controls
{
	/// <summary>
	/// <para>Control that display XSLT transformed XML.</para>
	/// <para><b>Author:</b> Logutov Michael<br />
	/// <b>Creation date:</b> 01 august 2008</para>
	/// </summary>
	public class XsltTransform : Control
	{
		#region Accessors

		#region Xslt

		/// <summary>
		/// <see cref="Xslt"/> internal field.
		/// </summary>
		private string _Xslt;

		/// <summary>
		/// (GET, SET) Website path to XSLT file. Default value is null.
		/// </summary>
		[Browsable (true)]
		[Bindable (false)]
		[Localizable (false)]
		[Description ("Website path to XSLT file")]
		[Category ("Data")]
		[DisplayName ("Xslt")]
		[DefaultValue (null)]
		[UrlProperty ("*.xslt")]
		public string Xslt
		{
			get
			{
				return this._Xslt;
			}
			set
			{
				this._Xslt = value;
			}
		}

		#endregion

		#region Data

		/// <summary>
		/// <see cref="Data"/> internal field.
		/// </summary>
		private string _Data;

		/// <summary>
		/// (GET, SET) XML data. Default value is null.
		/// </summary>
		[Browsable (true)]
		[Bindable (false)]
		[Localizable (false)]
		[Description ("XML data")]
		[Category ("Data")]
		[DisplayName ("Data")]
		[DefaultValue (null)]
		public string Data
		{
			get
			{
				return this._Data;
			}
			set
			{
				this._Data = value;
			}
		}

		#endregion

		#region DataXml

		/// <summary>
		/// <see cref="DataXml"/> internal field.
		/// </summary>
		private XElement _DataXml;

		/// <summary>
		/// (GET, SET) XML data object instance. If this property value is not null then it's content will be used instead of <see cref="Data"/> property value. Default value is null.
		/// </summary>
		[Browsable (false)]
		[Bindable (false)]
		[Localizable (false)]
		[Description ("XML data instance")]
		[Category ("Data")]
		[DisplayName ("Data")]
		[DefaultValue (null)]
		public XElement DataXml
		{
			get
			{
				return this._DataXml;
			}
			set
			{
				this._DataXml = value;
			}
		}

		#endregion

		#region Arguments

		/// <summary>
		/// <see cref="Arguments"/> internal field.
		/// </summary>
		private XsltArgumentList _Arguments = new XsltArgumentList ();

		/// <summary>
		/// (GET, SET) List of arguments that will be passed to XSLT transform. Default value is empty list.
		/// </summary>
		[Browsable (false)]
		public XsltArgumentList Arguments
		{
			get
			{
				return this._Arguments;
			}
		}

		#endregion

		#region UseXsltHelpers

		/// <summary>
		/// <see cref="UseXsltHelpers"/> internal field.
		/// </summary>
		private bool _UseXsltHelpers = false;

		/// <summary>
		/// (GET, SET) If true then various script helper objects will add as extension object to <see cref="Arguments"/>. Default value is false.
		/// </summary>
		[Browsable (true)]
		[Bindable (false)]
		[Localizable (false)]
		[Description ("If true then XsltScriptHelpers object will add as extension object to Arguments.")]
		[Category ("Data")]
		[DisplayName ("Use XSLT helpers")]
		[DefaultValue (false)]
		public bool UseXsltHelpers
		{
			get
			{
				return this._UseXsltHelpers;
			}
			set
			{
				this._UseXsltHelpers = value;
			}
		}

		#endregion

		#endregion

		/// <summary>
		/// Sends server control content to a provided <see cref="HtmlTextWriter"/> object, which writes the content to be rendered on the client.
		/// </summary>
		/// <param name="writer">The <see cref="HtmlTextWriter"/> object that receives the server control content.</param>
		protected override void Render (HtmlTextWriter writer)
		{
			if (!string.IsNullOrEmpty (this.Xslt) && (!string.IsNullOrEmpty (this.Data) || this.DataXml != null))
			{
				if (this.UseXsltHelpers)
					this.Arguments.AddExtensionObject ("urn:script", new XsltScriptHelpers ());

				if (this.DataXml != null)
					writer.Write (Utils.XsltTransform (this.DataXml.CreateNavigator (), this.Arguments, this.Xslt));
				else
					writer.Write (Utils.XsltTransform (this.Data, this.Arguments, this.Xslt));
			}

			base.Render (writer);
		}
	}
}