using System.Collections.Generic;
namespace PunkteInWedding.Models {
	/// <summary>
	/// Database type for Users 
	/// Also used in the app for receiving users
	/// </summary>
	public class User : System.IComparable<User> {
		/// <summary>
		/// Primary key. Used for Litedb
		/// </summary>
		public int Id { get; set; }
		/// <summary>
		/// Name of the user
		/// </summary>
		public string Username { get; set; }
		/// <summary>
		/// Password of the user
		/// </summary>
		public string Password { get; set; }
		/// <summary>
		/// Email address of the user
		/// </summary>
		public string Email { get; set; }
		/// <summary>
		/// Current score in points 
		/// </summary>
		public int Score { get; set; }
		/// <summary>
		/// required for the <see cref="System.IComparable{T}"/> Interface
		/// </summary>
		#region IComparable
		public int CompareTo(User other) => other == null ? throw new System.ArgumentNullException(nameof(other)) : Score.CompareTo(other.Score);
		/// <summary>
		/// required for the <see cref="System.IComparable{T}"/> Interface
		/// </summary>
		public override bool Equals(object obj) => obj != null && obj is User u ? Username == u.Username : false;
		/// <summary>
		/// Generate unique hash
		/// </summary>
		public override int GetHashCode() {
			var hashCode = -1715767847;
			hashCode = (hashCode * -1521134295) + Id.GetHashCode();
			hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Username);
			return hashCode;
		}
		public static bool operator ==(User user1, User user2) => EqualityComparer<User>.Default.Equals(user1, user2);
		public static bool operator !=(User user1, User user2) => !(user1 == user2);
		/// <summary>
		/// Create a copy of the username with the same information
		/// </summary>
		public User Clone() => new User() { Id = Id, Username = Username, Password = Password == null ? null : string.Copy(Password), Email = Email, Score = Score };
		#endregion
	}
}