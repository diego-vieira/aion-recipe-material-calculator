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
using System.Xml.XPath;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Net;
using System.Globalization;
using System.Text;
using System.Web.Caching;
using System.Text.RegularExpressions;

namespace Website
{
	#region class IdAndValue

	/// <summary>
	/// General object with two fields: Id (int) and Value (string).
	/// </summary>
	public class IdAndValue
	{
		public int Id { get; set; }
		public string Value { get; set; }
	}

	#endregion

	/// <summary>
	/// <para>Data manager helper class.</para>
	/// <para><b>Author:</b> Logutov Michael<br />
	/// <b>Creation date:</b> 05 august 2009</para>
	/// </summary>
	public class DataManager : IDisposable
	{
		#region Accessors

		#region Db

		/// <summary>
		/// <see cref="Db"/> internal field.
		/// </summary>
		private DbDataContext _Db;

		/// <summary>
		/// (GET, SET) DB data context object instance. This object instance cached in current <see cref="HttpContext"/> items. Default value is null.
		/// </summary>
		public DbDataContext Db
		{
			get
			{
				return this._Db;
			}
			set
			{
				this._Db = value;
			}
		}

		#endregion

		#region CommandTimeout

		/// <summary>
		/// <see cref="CommandTimeout"/> internal field.
		/// </summary>
		private int _CommandTimeout = 60;

		/// <summary>
		/// (GET, SET) The time in seconds to wait for the command to execute. Default value is 60.
		/// </summary>
		public int CommandTimeout
		{
			get
			{
				return this._CommandTimeout;
			}
			set
			{
				this._CommandTimeout = value;
				this.Db.CommandTimeout = value;
			}
		}

		#endregion

		#endregion

		#region Direct SQL methods

		/// <summary>
		/// Executes SQL statement.
		/// </summary>
		/// <typeparam name="T">Type of return value.</typeparam>
		/// <param name="db">Data context.</param>
		/// <param name="sql">SQL statement.</param>
		/// <param name="args">List of arguments.</param>
		/// <returns>Return value.</returns>
		public T ExecuteScalar<T> (DataContext db, string sql, params object[] args)
		{
			return db.ExecuteQuery<T> (sql, args).FirstOrDefault ();
		}

		/// <summary>
		/// Executes SQL statement using <see cref="SqlCommand.ExecuteNonQuery"/>.
		/// </summary>
		/// <param name="db">Data context.</param>
		/// <param name="sql">SQL statement.</param>
		/// <param name="args">List of name-value arguments pairs.</param>
		/// <returns>Return value.</returns>
		public int ExecuteNonQuery (DataContext db, string sql, params object[] args)
		{
			return db.ExecuteCommand (sql, args);
		}

		/// <summary>
		/// Executes SQL statement.
		/// </summary>
		/// <typeparam name="T">Type of return value.</typeparam>
		/// <param name="db">Data context.</param>
		/// <param name="sql">SQL statement.</param>
		/// <param name="args">List of name-value arguments pairs.</param>
		/// <returns>Return value.</returns>
		public List<T> ExecuteScalarList<T> (DataContext db, string sql, params object[] args)
		{
			return db.ExecuteQuery<T> (sql, args).ToList ();
		}

		#endregion

		#region Init & destruct

		/// <summary>
		/// Default constructor.
		/// </summary>
		protected DataManager ()
		{
			// db
			{
				this.Db = HttpContext.Current.Items["Db"] as DbDataContext;
				if (this.Db == null)
				{
					this.Db = DataContextFactory.Create<DbDataContext> ("Db");
					HttpContext.Current.Items["Db"] = this.Db;
				}

				this.Db.CommandTimeout = this.CommandTimeout;
			}
		}

		/// <summary>
		/// Creates instance of data manager class.
		/// </summary>
		/// <returns>Instance of data manager class.</returns>
		public static DataManager GetInstance ()
		{
			DataManager res = HttpContext.Current.Items["DataManager"] as DataManager;
			if (res == null)
			{
				res = new DataManager ();
				HttpContext.Current.Items["DataManager"] = res;
			}

			return res;
		}

		/// <summary>
		/// Release all resources.
		/// </summary>
		public void Dispose ()
		{
			if (this.Db != null)
			{
				try
				{
					this.Db.Dispose ();
				}
				catch
				{
				}
				this.Db = null;
			}

			HttpContext.Current.Items.Remove ("DataManager");
		}

		#endregion
	}
}