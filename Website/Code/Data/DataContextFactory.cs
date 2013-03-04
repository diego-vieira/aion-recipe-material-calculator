using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using System.Web;
using System.Configuration;
using System.ComponentModel;

namespace Website
{
	#region class DataContextRecreatedEventArgs

	/// <summary>
	/// Custom <see cref="EventArgs"/> implementation that will be used in <see cref="DataContextFactory.DataContextRecreated"/> event.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DataContextRecreatedEventArgs : EventArgs
	{
		#region Accessors

		#region OldDataContext

		/// <summary>
		/// <see cref="OldDataContext"/> internal field.
		/// </summary>
		private DataContext _OldDataContext;

		/// <summary>
		/// (GET) Old instance of the data context object that was recreated. Default value is null.
		/// </summary>
		public DataContext OldDataContext
		{
			get
			{
				return this._OldDataContext;
			}
			protected set
			{
				this._OldDataContext = value;
			}
		}

		#endregion

		#region NewDataContext

		/// <summary>
		/// <see cref="NewDataContext"/> internal field.
		/// </summary>
		private DataContext _NewDataContext;

		/// <summary>
		/// (GET) New data context object instance that was recreated. Default value is null.
		/// </summary>
		public DataContext NewDataContext
		{
			get
			{
				return this._NewDataContext;
			}
			protected set
			{
				this._NewDataContext = value;
			}
		}

		#endregion

		#endregion

		/// <summary>
		/// Default constructor with initialization.
		/// </summary>
		/// <param name="oldDataContext">Inital value for <see cref="OldDataContext"/> property.</param>
		/// <param name="newDataContext">Inital value for <see cref="NewDataContext"/> property.</param>
		public DataContextRecreatedEventArgs (DataContext oldDataContext, DataContext newDataContext)
		{
			this.OldDataContext = oldDataContext;
			this.NewDataContext = newDataContext;
		}
	}

	#endregion

	/// <summary>
	/// <para>Factory for managing <see cref="DataContext"/> objects.</para>
	/// <para><b>Author:</b> Logutov Michael<br />
	/// <b>Creation date:</b> 18 april 2008</para>
	/// </summary>
	public static class DataContextFactory
	{
		private const string CreatedDataContextKeys = "DataContextFactory.CreatedDataContextKeys";
		private const string DataContextRecreatedEventKey = "DataContextFactory.DataContextRecreatedEvent";
		private static readonly object DataContextRecreatedEventListKey = new object ();

		#region Events

		/// <summary>
		/// Get event list for <see cref="DataContextFactory.DataContextRecreated"/> event.
		/// </summary>
		/// <returns><see cref="EventHandlerList"/> object instance.</returns>
		private static EventHandlerList GetDataContextRecreatedEventList ()
		{
			EventHandlerList events = HttpContext.Current.Items[DataContextFactory.DataContextRecreatedEventKey] as EventHandlerList;
			if (events == null)
			{
				events = new EventHandlerList ();
				HttpContext.Current.Items[DataContextFactory.DataContextRecreatedEventKey] = events;
			}

			return events;
		}

		/// <summary>
		/// Event that will be fired when certain data context was recreated.
		/// </summary>
		public static event EventHandler<DataContextRecreatedEventArgs> DataContextRecreated
		{
			add
			{
				DataContextFactory.GetDataContextRecreatedEventList ().AddHandler (DataContextFactory.DataContextRecreatedEventListKey, value);
			}
			remove
			{
				DataContextFactory.GetDataContextRecreatedEventList ().RemoveHandler (DataContextFactory.DataContextRecreatedEventListKey, value);
			}
		}

		#endregion

		/// <summary>
		/// Gets data context key which will be used to store created data context in <see cref="HttpContext"/>.
		/// </summary>
		/// <returns>Data context key string.</returns>
		private static string GetDataContextKey (Type typeDataContext) 
		{
			return "DataContextFactory." + typeDataContext.Name;
		}

		/// <summary>
		/// Add created data context key to internal list of all created data contexts keys.
		/// </summary>
		/// <param name="key">Key.</param>
		private static void AddCreatedDataContextKey (string key)
		{
			List<string> created_data_context_keys = HttpContext.Current.Items[DataContextFactory.CreatedDataContextKeys] as List<string>;
			if (created_data_context_keys == null)
				created_data_context_keys = new List<string> ();

			if (!created_data_context_keys.Contains (key))
				created_data_context_keys.Add (key);

			HttpContext.Current.Items[DataContextFactory.CreatedDataContextKeys] = created_data_context_keys;
		}

		/// <summary>
		/// Creates <see cref="DataContext"/> instance with "Default" connection string. If the one exists in current <see cref="HttpContext"/> then no new object will be created and the existed one will be returned.
		/// </summary>
		/// <typeparam name="T">Type of <see cref="DataContext"/>.</typeparam>
		/// <returns><see cref="DataContext"/> instance with default connection string.</returns>
		public static T Create<T> (string connectionName) where T: DataContext
		{
			return DataContextFactory.Create<T> (false, connectionName);
		}
		
		/// <summary>
		/// Creates <see cref="DataContext"/> instance with specified <paramref name="connectionStringName"/>. If the one exists in current <see cref="HttpContext"/> then no new object will be created and the existed one will be returned.
		/// </summary>
		/// <typeparam name="T">Type of <see cref="DataContext"/>.</typeparam>
		/// <param name="forceNew">If true then new data context will be created regardless of existed in <see cref="HttpContext"/>.</param>
		/// <param name="connectionStringName">Name of the connection string to use.</param>
		/// <returns><see cref="DataContext"/> instance with default connection string.</returns>
		public static T Create<T> (bool forceNew, string connectionStringName) where T : DataContext
		{
			return (T)Create(forceNew, connectionStringName, typeof(T));
		}

		public static DataContext Create (bool forceNew, string connectionStringName,Type dataContextType)
		{
			// create data context
			string key = DataContextFactory.GetDataContextKey(dataContextType);
			List<DataContext> dbs = HttpContext.Current.Items[key] as List<DataContext>;

			if (dbs == null)
				dbs = new List<DataContext> ();

			DataContext db;
			if (dbs.Count < 1 || forceNew)
			{
				if (string.IsNullOrEmpty (connectionStringName))
					throw new ArgumentNullException ("connectionStringName");

				ConnectionStringSettings connection_string = ConfigurationManager.ConnectionStrings[connectionStringName];
				if (connection_string == null)
					throw new InvalidOperationException ("Connection string '" + connectionStringName + "' not found.");

				db = (DataContext) Activator.CreateInstance (dataContextType, connection_string.ConnectionString);
				dbs.Add (db);
			}
			else
				db = (DataContext) dbs[0];

			HttpContext.Current.Items[key] = dbs;
			DataContextFactory.AddCreatedDataContextKey (key);

			return db;
		}

		/// <summary>
		/// Recreate data context and fire <see cref="DataContextRecreated"/>. Note that this method will automatically call <see cref="DataContext.Dispose"/> for <paramref name="oldDataContext"/> at the end of it's execution.
		/// </summary>
		/// <typeparam name="T">Type of <see cref="DataContext"/>.</typeparam>
		/// <param name="oldDataContext">Old instance of data context that must be recreated.</param>
		/// <returns>Recreated <see cref="DataContext"/> instance.</returns>
		public static T Recreate<T> (T oldDataContext) where T : DataContext
		{
			string key = DataContextFactory.GetDataContextKey(typeof(T));
			List<DataContext> dbs = HttpContext.Current.Items[key] as List<DataContext>;

			if (dbs == null)
				dbs = new List<DataContext> ();

			T db = (T) Activator.CreateInstance (typeof (T), oldDataContext.Connection);
			int old_index = dbs.IndexOf (oldDataContext);

			if (old_index < 0)
				dbs.Add (db);
			else
				dbs[old_index] = db;

			HttpContext.Current.Items[key] = dbs;
			DataContextFactory.AddCreatedDataContextKey (key);

			EventHandlerList events = DataContextFactory.GetDataContextRecreatedEventList ();
			Delegate d = events[DataContextFactory.DataContextRecreatedEventListKey];
			if (d != null)
				d.DynamicInvoke (null, new DataContextRecreatedEventArgs (oldDataContext, db));

			oldDataContext.Dispose ();

			return db;
		}

		/// <summary>
		/// Close and dispose all created by this factory <see cref="DataContext"/> objects.
		/// </summary>
		public static void CloseAll ()
		{
			List<string> created_data_context_keys = HttpContext.Current.Items[DataContextFactory.CreatedDataContextKeys] as List<string>;
			if (created_data_context_keys != null)
			{
				foreach (string key in created_data_context_keys)
				{
					List<DataContext> dbs = HttpContext.Current.Items[key] as List<DataContext>;
					if (dbs != null)
					{
						foreach (DataContext db in dbs)
							db.Dispose ();

						dbs.Clear ();
						HttpContext.Current.Items.Remove (key);
					}
				}

				HttpContext.Current.Items.Remove (DataContextFactory.CreatedDataContextKeys);
			}
		}
	}
}
