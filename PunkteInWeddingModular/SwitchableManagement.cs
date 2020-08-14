using System.Threading;
using System.Threading.Tasks;
namespace System {
	/// <summary>
	/// A Class that Stores Cached versions of data.
	/// This can boost queries etc. MASSIVELY
	/// <para>
	/// The class works on a V-Sync like AB double buffering system.
	/// It always exposes one buffer and swaps buffers in the background when the data is refreshed.
	/// </para>
	/// THIS IS A READ ONLY SYSTEM! changes made to the returned buffers are not applied on the data source
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SwitchableManagement<T> {
		/// <summary>
		/// A delegate type used for updating/populating the content of a given buffer
		/// </summary>
		/// <param name="t"></param>
		public delegate void Updater(ref T t);
		/// <summary>
		/// storage buffers
		/// </summary>
		T A, B;
		/// <summary>
		/// Indicates which buffer gets exposed
		/// </summary>
		bool useA = true;
		/// <summary>
		/// indicates if a update <see cref="Task"/> should run
		/// </summary>
		bool runUpdates = false;
		/// <summary>
		/// Duration between updates
		/// </summary>
		TimeSpan updateDelay;
		/// <summary>
		/// Shows whether the buffers are currently being updated or not.
		/// </summary>
		public bool IsBusy { get; protected set; } = false;
		/// <summary>
		/// Storage for our updater
		/// </summary>
		private readonly Updater UpdateHandler;
		/// <summary>
		/// Create a new instance of a <see cref="SwitchableManagement{T}"/>
		/// Please supply two empty instances of your collection / type for the updater to use
		/// </summary>
		/// <param name="startA">Source instance for Buffer A</param>
		/// <param name="startB">Source instance for Buffer B</param>
		/// <param name="updater">Update Task</param>
		public SwitchableManagement(T startA, T startB, Updater updater) {
			A = startA;
			B = startB;
			UpdateHandler = updater;
			Update();
		}
		/// <summary>
		/// Get the current buffer
		/// </summary>
		public T Get => useA ? A : B;
		/// <summary>
		/// Thread for Updating buffers asynchronously
		/// </summary>
		/// <param name="e"></param>
		private protected void Thread() {
			try {
				if (useA)
					UpdateHandler(ref B);
				else
					UpdateHandler(ref A);
				useA = !useA;
			}
			catch (Exception ex) {
				C.WriteLineE(ex);
			}
			finally {
				IsBusy = false;
			}
		}
		/// <summary>
		/// Manually causes the buffers to update
		/// </summary>
		public void Update() {
			lock (this) {
				if (IsBusy)
					return;
				IsBusy = true;
			}
			ServerFrameWork.QUWI("SwMa<" + typeof(T).Name + ">::Update", Thread);
		}
		/// <summary>
		/// Starts a Update thread in the background, which updates the contents of the buffer regularly.
		/// Can be used to change the <see cref="TimeSpan"/> between Updates without causing any issues
		/// </summary>
		/// <param name="updateDelay">Time between automatic updates</param>
		public async void DoRegularUpdates(TimeSpan updateDelay) {
			this.updateDelay = updateDelay;
			if (runUpdates == false) {
				runUpdates = true;
				while (runUpdates) {
					await Task.Delay(this.updateDelay).ConfigureAwait(true);
					if (runUpdates)
						Update();
				}

			}
		}
		/// <summary>
		/// Stops the regular updates.
		/// Since the Update task relies on sleeping this will only apply when the last <see cref="TimeSpan"/> is over
		/// </summary>
		public void StopRegularUpdates() => runUpdates = false;
	}
}
