namespace PunkteInWedding.Models {
	/// <summary>
	/// Tiny log file format for transferring to and displaying logs in the app
	/// </summary>
	public class TinyLog {
		/// <summary>
		/// Tick count from the time when this log has been created
		/// Needs to be converted back to a time representation
		/// </summary>
		public string Time { get; set; }
		/// <summary>
		/// Name of the user who waited on you
		/// </summary>
		public string Name { get; set; }
	}
}
