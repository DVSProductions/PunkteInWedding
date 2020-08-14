using PunkteInWedding.Models;
namespace System {
	/// <summary>
	/// Database type for Log entries
	/// </summary>
	public class Logentry {
		/// <summary>
		/// Unique identifier of this entry
		/// </summary>
		public int Id { get; set; }
		/// <summary>
		/// ID of the user that was waited on
		/// </summary>
		public int WatingOn { get; set; }
		/// <summary>
		/// ID of the user that was waiting
		/// </summary>
		public int Waitee { get; set; }
		/// <summary>
		/// Exact time when this event happened
		/// </summary>
		public DateTime When { get; set; }
		/// <summary>
		/// Convert this into a <see cref="TinyLog"/>.
		/// Please resolve the <see cref="Waitee"/>'s username yourself as this only knows IDs
		/// </summary>
		/// <param name="name">Username Resolved from this entries <see cref="Waitee"/></param>
		public TinyLog GetTinyLog(string name) => new TinyLog() { Name = name, Time = When.Ticks.ToString() };
		/// <summary>
		/// Returns a basic string representation of this event
		/// </summary>
		public override string ToString() => $"[{When}]: {Waitee} waited on {WatingOn}";
	}
}
