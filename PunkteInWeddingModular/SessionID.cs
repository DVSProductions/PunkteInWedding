using System.Security.Cryptography;
namespace System {
	/// <summary>
	/// Database type for Session IDS
	/// </summary>
	public class SessionID {
		/// <summary>
		/// Unique identifier. Not actual session ID
		/// </summary>
		public int Id { get; set; }
		/// <summary>
		/// ID of the user that this session is associated with.
		/// Fake Sessions have their <see cref="UserId"/> below 0.
		/// </summary>
		public int UserId { get; set; }
		/// <summary>
		/// Actual Session ID
		/// </summary>
		public string SID { get; set; }
		/// <summary>
		/// Creation time of the session
		/// </summary>
		public DateTime Created { get; set; }
		/// <summary>
		/// Time when the session is going to die.
		/// Once this Point has been exceeded the Session should no longer be in use
		/// </summary>
		public DateTime DeathOn { get; set; }
		/// <summary>
		/// Session ID length
		/// </summary>
		private static int Size => 64;
		/// <summary>
		/// When the session is used this is used to extend the lifetime of the Session ID
		/// </summary>
		private static readonly TimeSpan extendDeadline = new TimeSpan(14, 0, 0, 0);
		/// <summary>
		/// How long until a session Expires
		/// </summary>
		private static readonly TimeSpan lifetime = new TimeSpan(30, 0, 0, 0);
		/// <summary>
		/// ONLY FOR LITEDB DO NOT USE
		/// </summary>
		public SessionID() { }
		/// <summary>
		/// Creates a new Session and it's user ID
		/// </summary>
		/// <param name="userId">ID of the user name</param>
		/// <param name="randomSource">Generator to be used</param>
		public SessionID(int userId, RNGCryptoServiceProvider randomSource) {
			if(randomSource == null)
				throw new ArgumentNullException(nameof(randomSource));
			UserId = userId;
			var rnBytes = new byte[Size];
			randomSource.GetBytes(rnBytes);
			SID = Convert.ToBase64String(rnBytes).Replace('+', 'a').Replace('/', 'b');
			Created = DateTime.Now;
			DeathOn = Created + lifetime;
		}
		/// <summary>
		/// Expands the lifetime of a session if it nears death
		/// Returns whether the lifetime has been extended.
		/// </summary>
		public bool ExtendLiftetime() {
			if(DateTime.Now < DeathOn && DateTime.Now > DeathOn - extendDeadline) {
				DeathOn += lifetime;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Returns whether the Session has expired
		/// </summary>
		public bool IsExpired() => DeathOn <= DateTime.Now;
	}
}
