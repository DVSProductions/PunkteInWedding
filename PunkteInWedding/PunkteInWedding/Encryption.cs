using System.IO;
using System.Text;
namespace System.Security.Cryptography {
	/// <summary>
	/// Server and client side encryption methods
	/// </summary>
	static class Encryption {
		/// <summary>
		/// Salt for all encryptions
		/// </summary>
		private static readonly byte[] salt = new byte[] { 49, 103, 216, 55, 237, 139, 38, 192, 142, 81, 178, 208, 84 };
		/// <summary>
		/// MD5 is no longer safe. The implementation will now use SHA512
		/// Hashes the given byte array
		/// </summary>
		/// <param name="input">Byte array to hash</param>
		public static byte[] CalculateMD5Hash(byte[] input) {
			using(var hasher = SHA512.Create()) return hasher.ComputeHash(input);
		}
		/// <summary>
		/// MD5 is no longer safe. The implementation will now use SHA512
		/// Hashes the given <see cref="string"/> as <see cref="Encoding.ASCII"/>
		/// </summary>
		/// <param name="input">Input string to hash</param>
		/// <returns></returns>
		public static string CalculateMD5Hash(string input) => BitConverter.ToString(CalculateMD5Hash(Encoding.ASCII.GetBytes(input)));
		public static string Encrypt(string clearText, string Key) {
			var clearBytes = Encoding.Unicode.GetBytes(clearText);
			using(var encryptor = Aes.Create()) {
				using(var pdb = new Rfc2898DeriveBytes(Key, salt)) {
					encryptor.Key = pdb.GetBytes(32);
					encryptor.IV = pdb.GetBytes(16);
					using(var ms = new MemoryStream()) {
						using(var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)) {
							cs.Write(clearBytes, 0, clearBytes.Length);
							cs.Close();
						}
						return Convert.ToBase64String(ms.ToArray());
					}
				}
			}
		}
		/// <summary>
		/// Combines the password and unsername to generate a unique Passkey
		/// </summary>
		public static string EncryptPassword(string username, string pw) => 
			Encrypt(pw, Encrypt(CalculateMD5Hash(username.ToLower()), CalculateMD5Hash(pw))).Replace('+', 'a');
	}
}
