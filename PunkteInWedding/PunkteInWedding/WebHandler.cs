using Newtonsoft.Json;
using PunkteInWedding.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace PunkteInWedding {
	/// <summary>
	/// Helper class for various Network/ login/ messaging taks
	/// </summary>
	public static class WebHandler {
		/// <summary>
		/// Updates your score Immediately once called
		/// </summary>
		public static Action UpdateMe;
		/// <summary>
		/// references a method that will check if the login has been completed and then shows 
		/// either the login or the main page
		/// </summary>
		public static Action DaddyLogoutDetection;
		/// <summary>
		/// Sid file presence cache. reduces IOPS
		/// </summary>
		private static bool? hasSIDFile;
		/// <summary>
		/// Current Session
		/// </summary>
		private static string sid = null;

		public static string shareUsername = null;
		public const int port = 50_001;
		public const string port_s = "50001";
		private const string strs_pwd = "&password=";
		public const string Domain = "dvsproductions.de", ServerURL = "https://" + Domain + ":" + port_s + "/piw/";
		public static readonly string SIDFILE = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/PIW.sid";
		public static bool HasSIDFILE => hasSIDFile.HasValue ? hasSIDFile == true : File.Exists(SIDFILE);
		public static string SID { get { if(sid == null) sid = File.ReadAllText(SIDFILE); return sid; } }
		public static Task<HttpResponseMessage> Send(string s) => new HttpClient().SendAsync(new HttpRequestMessage() {
			RequestUri = new Uri(ServerURL + s),
			Method = HttpMethod.Get
		});
		public static async Task<bool> Ping(string ip, short port) {
			try {
				//Console.WriteLine("Pinging Port");
				using(var a = new System.Net.Sockets.TcpClient()) {
					var b = a.BeginConnect(ip, port, null, null);
					await Task.Delay(1000);
					//Console.WriteLine("Timeout Over");
					if(b.AsyncWaitHandle.WaitOne(10)) {
						a.EndConnect(b);
						//Console.WriteLine("Ended connect");
						return true;
					}
					//Console.WriteLine("Connected Fine");
					return false;
				}
			}
			catch {
				return false;
			}
		}
		public static bool PingSync(string ip, ushort port) {
			try {
				using(var a = new System.Net.Sockets.TcpClient()) {
					var b = a.BeginConnect(ip, port, null, null);
					for(var n = 0; n < 10; n++)
						if(b.AsyncWaitHandle.WaitOne(75)) {
							a.EndConnect(b);
							return true;
						}
					return false;
				}
			}
			catch {
				return false;
			}
		}
		private static async Task<PingReply> SendPingAsync(string ip) {
			var p = new Ping();
			var ret = false;
			PingReply reply = null;
			p.PingCompleted += (a, b) => { reply = b.Reply; ret = true; };
			p.SendAsync(ip, null);
			while(!ret)
				await Task.Delay(100);
			return reply;
		}/*
		public static async Task<bool> Ping(string ip) {
			try {
				var resp = (await SendPingAsync(ip)).Status;
				for (int i = 4; resp == IPStatus.TimedOut && i > 0; i--)
					resp = (await SendPingAsync(ip)).Status;
				return resp == IPStatus.Success;
			}
			catch {
				return false;
			}
		}
		*/
		public static bool Ping(string ip) {
			try {
				var resp = (new Ping().Send(ip)).Status;
				for(var i = 4; resp == IPStatus.TimedOut && i > 0; i--)
					resp = (new Ping().Send(ip)).Status;
				return resp == IPStatus.Success || resp == IPStatus.TtlExpired;
			}
			catch {
				return false;
			}
		}
		public static async Task<bool> PingDVS() {
			try {
				return (await (await Send("ping")).Content.ReadAsStringAsync()) == "Pong!";
			}
			catch {
				return false;
			}
		}
		public static async Task<bool> HasInternet() => Ping(Domain) && await PingDVS();

		public static async Task<bool> TestInternet(Xamarin.Forms.Page whosAsking) {
			try {
				if(!await HasInternet()) {
					await whosAsking.DisplayAlert("Fehler", "Wir konnten keine Verbindung mit dem Server herstellen", "Schade");
					return false;
				}
			}
			catch {
				await whosAsking.DisplayAlert("Fehler", "Wir konnten keine Verbindung mit dem Server herstellen", "Schade");
				return false;
			}
			return true;

		}
		static async Task Autologout(HttpResponseMessage msg) {
			if(msg.StatusCode == HttpStatusCode.Unauthorized)
				await Logout();
		}
		public static async Task<HttpStatusCode> Login(User u) {
			var resp = await Send("login?username=" + u.Username + strs_pwd + Encryption.EncryptPassword(u.Username, u.Password));
			if(resp.StatusCode == HttpStatusCode.OK)
				File.WriteAllText(SIDFILE, await resp.Content.ReadAsStringAsync());
			hasSIDFile = null;
			return resp.StatusCode;
		}
		public static async Task<RegisterErrorState> Register(User u) {
			var resp = await Send("register?username=" + u.Username + strs_pwd + Encryption.EncryptPassword(u.Username, u.Password) + "&email=" + u.Email);
			if(resp.StatusCode == HttpStatusCode.OK)
				return RegisterErrorState.Success;
			var msg = await resp.Content.ReadAsStringAsync();
			return msg.Length != 1 ? RegisterErrorState.Fail : (RegisterErrorState) msg[0];
		}
		public static User CreateUser(string username, string email, string password) => new User() { Username = username, Email = email, Password = password };
		/// <summary>
		/// Logs the user out locally. 
		/// Does not invalidate the session.
		/// </summary>
		static void LocalLogout() { if(HasSIDFILE) File.Delete(SIDFILE); hasSIDFile = null; sid = null; shareUsername = null; UpdateMe = null; DaddyLogoutDetection(); }
		/// <summary>
		/// Attempts to log the user out by sending a logout request.
		/// Then performs a local logout
		/// </summary>
		public static async Task Logout() { if(await HasInternet()) await Send("logout?sid=" + SID); LocalLogout(); }
		/// <summary>
		/// Gets the current <see cref="User"/> information from the server
		/// </summary>
		public static async Task<User> GetMe() {
			var resp = await Send("me?sid=" + SID);
			await Autologout(resp);
			return resp.StatusCode != HttpStatusCode.OK
			? null
			: JsonConvert.DeserializeObject<User>(await resp.Content.ReadAsStringAsync());
		}
		/// <summary>
		/// Gets the current scoreboard from the server
		/// </summary>
		public static async Task<List<User>> GetScoreboard() {
			var resp = await Send("scoreboard?sid=" + SID);
			await Autologout(resp);
			return resp.StatusCode != HttpStatusCode.OK
			? null
			: JsonConvert.DeserializeObject<List<User>>(await resp.Content.ReadAsStringAsync());
		}
		/// <summary>
		/// Gets the <see cref="TinyLog"/> list that
		/// </summary>
		/// <returns></returns>
		public static async Task<List<TinyLog>> GetRecentActions() {
			var resp = await Send("tinyLog?sid=" + SID);
			await Autologout(resp);
			return resp.StatusCode != HttpStatusCode.OK
			? null
			: JsonConvert.DeserializeObject<List<TinyLog>>(await resp.Content.ReadAsStringAsync());
		}
		public static async Task WaitOn(int id) {
			await Autologout(await Send("wait?sid=" + SID + "&target=" + id));
			UpdateMe?.Invoke();
		}
		public static async Task RestoreAccount(string email) => await Send("restore?email=" + email);
		public static bool IsValidEmail(string email) => System.Text.RegularExpressions.Regex.IsMatch(email, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,5})+)$");
		public static class EditAcc {
			private static string Url(string username, string password) => "EditAcc?sid=" + SID + strs_pwd + Encryption.EncryptPassword(username, password);
			public static async Task<bool> PWCheck(string pw) => string.IsNullOrWhiteSpace(pw) ? false : (await Send(Url(shareUsername, pw))).StatusCode == HttpStatusCode.OK;
			public static async Task<User> GetUserData(string pw, string username) {
				var resp = await Send(Url(username, pw));
				return resp.StatusCode != HttpStatusCode.OK ? null : JsonConvert.DeserializeObject<User>(await resp.Content.ReadAsStringAsync());
			}
			public static async Task<RegisterErrorState> UpdateUser(string username, string newUsername, string password, string newPassword, string email) {
				var resp = await Send(Url(username, password) + (string.IsNullOrWhiteSpace(newPassword) || password == newPassword ? "" : "&newPassword=" + Encryption.EncryptPassword(newUsername ?? username, newPassword)) + (newUsername == null || newUsername == username ? "" : "&newUsername=" + newUsername) + "&email=" + email);
				return resp.StatusCode == HttpStatusCode.OK ? RegisterErrorState.Success : (RegisterErrorState) (byte) (await resp.Content.ReadAsStringAsync())[0];
			}
			public static async Task<bool> DeleteUser(string password) {
				if((await Send("DeleteAcc?sid=" + SID + strs_pwd + Encryption.EncryptPassword(shareUsername, password))).StatusCode == HttpStatusCode.OK) {
					LocalLogout();
					return true;
				}
				return false;
			}
		}
		public static bool CheckJsonWorking() {
			var o = new User() { Email = "test@gmail.com", Id = 10, Password = "ABCasidnas==", Score = 100, Username = "Kek" };
			var str = JsonConvert.SerializeObject(o);
			var d = JsonConvert.DeserializeObject<User>(str);
			return o.Username == d.Username && o.Email == d.Email && o.Id == d.Id && o.Password == d.Password && o.Score == d.Score;
		}
	}
}
