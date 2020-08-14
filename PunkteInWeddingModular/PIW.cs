using PunkteInWedding.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net;
using System.Web.Helpers;
namespace PunkteInWeddingModular {
	/// <summary>
	/// PIW Server implementation. 
	/// This is the Class that holds all main logic
	/// </summary>
	[Export(typeof(IServer))]
	[ExportMetadata("Name", "Punkte in Wedding Server")]
	[ExportMetadata("BasePath", "piw")]
	public partial class PIW : IServer {
		/// <summary>
		/// We don't have a custom catchall page
		/// </summary>
		public ServerFrameWork.respoMethod Catchall => null;
		/// <summary>
		/// We don't have a custom error page
		/// </summary>
		public ServerFrameWork.errorPage ErrorPage => null;
		public List<ICommand> AvaliableCommands => new List<ICommand>() {
			new ListCommand(this),
			new SimpleCommand("fullTrace","Does a full log trace of everyones points and blocks everyones access while doing so",(a)=>DB.Fulltrace())
		};
		/// <summary>
		/// Database instance
		/// </summary>
		protected Database DB { get; private set; }
		public Dictionary<string, ServerFrameWork.respoMethod> PathsWithResponders => new Dictionary<string, ServerFrameWork.respoMethod>(){
			{"login",       Login },
			{"register",    Register },
			{"logout",      Logout },
			{"scoreboard",  ScoreBoard },
			{"tinyLog",     RecentActivity },
			{"wait",        Wait },
			{"me",          Me },
			{"restore",     Restore},
			{"EmailValidation",         EmailValidation },
			{"EmailValidation/Delete",  DeleteValidation },
			{"EditAcc",     EditAcc },
			{"DeleteAcc",   DelAcc },
			{"ping",        Ping },
			{"",            WelcomePage }
		};
		/// <summary>
		/// When the server is initialized we only need to setup the database
		/// </summary>
		public void Init() => DB = new Database();
		/// <summary>
		/// Handles the login page. 
		/// Returns a session id when the login was successful
		/// </summary>
		private string Login(HttpListenerRequest request, HttpListenerResponse response) {
			var u = CreateAndVerifyUser(request.QueryString, false);
			if (u == null) {
				response.StatusCode = 400;
				return "";
			}
			if (DB.Login(u, out var SID))
				return SID;
			response.StatusCode = 401;
			return "";
		}
		/// <summary>
		/// Retrieves information from the query to generate a <see cref="User"/>
		/// </summary>
		/// <param name="nv">NameValueCollection from the http query</param>
		private static User CreateUserFromQuery(System.Collections.Specialized.NameValueCollection nv) {
			var u = new User();
			foreach (var k in nv.AllKeys) {
				var l = k.ToLower();
				switch (l) {
					case ("username"):
						u.Username = nv.Get(k);
						break;
					case ("password"):
						u.Password = nv.Get(k);
						break;
					case ("email"):
						u.Email = nv.Get(k);
						break;
					default:
						break;
				}
			}
			return u;
		}
		/// <summary>
		/// Tries to load a user from the Query and checks if the data is valid / reasonable
		/// </summary>
		/// <param name="nv">NameValueCollection from the http query</param>
		/// <param name="useEmail">If true, the email is required</param>
		/// <returns></returns>
		private static User CreateAndVerifyUser(System.Collections.Specialized.NameValueCollection nv, bool useEmail = true) {
			var u = CreateUserFromQuery(nv);
			return string.IsNullOrWhiteSpace(u.Password) || string.IsNullOrWhiteSpace(u.Username) || (useEmail && string.IsNullOrWhiteSpace(u.Email))
				? null
				: u;
		}
		/// <summary>
		/// Handles the register page. 
		/// Tries to register a given user.
		/// Sets Status codes for the different error states and returns the appropriate char
		/// </summary>
		private string Register(HttpListenerRequest request, HttpListenerResponse response) {
			var u = CreateAndVerifyUser(request.QueryString);
			if (u == null) {
				response.StatusCode = 400;
				return Html.BodyBuilder(Html.h1("Invalid Register Page"));
			}
			var result = DB.Register(u);
			switch (result) {
				case RegisterErrorState.Success:
					response.StatusCode = 201;
					break;
				case RegisterErrorState.EmailFound:
					response.StatusCode = 409;
					break;
				case RegisterErrorState.NameFound:
					response.StatusCode = 409;
					break;
				default:
					response.StatusCode = 500;
					break;
			}
			return "" + (char)result;
		}
		/// <summary>
		/// Ensures that the user in the Query is valid
		/// </summary>
		/// <returns><see langword="true"/>if valid</returns>
		private bool ValidateLogin(System.Collections.Specialized.NameValueCollection nv) {
			var sesh = DB.FindSession(nv?.Get("sid"));
			return sesh == null || sesh.UserId < 0 ? false : sesh?.IsExpired() == false;
		}
		/// <summary>
		/// Ensures that the user in the Query is valid
		/// outputs the users UID once found.
		/// </summary>
		/// <returns><see langword="true"/>if valid</returns>
		private bool ValidateLogin(System.Collections.Specialized.NameValueCollection nv, out int UID) {
			var s = DB.FindSession(nv?.Get("sid"));
			UID = s?.UserId ?? default;
			return s.IsExpired() == false;
		}
		/// <summary>
		/// Handles the ScoreBoard page. 
		/// Validates the login and then returns a JSON encoded Scoreboard
		/// </summary>
		private string ScoreBoard(HttpListenerRequest request, HttpListenerResponse response) {
			if (!ValidateLogin(request.QueryString)) {
				response.StatusCode = 401;
				return "";
			}
			response.ContentType = "application/json";
			return Json.Encode(DB.GetScoreboard());
		}
		/// <summary>
		/// Simply replies "Pong!"
		/// For testing purposes
		/// </summary>
		private string Ping(HttpListenerRequest request, HttpListenerResponse response) => "Pong!";
		/// <summary>
		/// Handles the Logout page. 
		/// This will immediately invalidate the session of the user that sent this request.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		private string Logout(HttpListenerRequest request, HttpListenerResponse response) {
			DB.InvalidateSession(request.QueryString.Get("sid"));
			return "";
		}
		/// <summary>
		/// Handles the Account settings page.
		/// Allows the user to change their username, password or email.
		/// Returns error codes corresponding with what went wrong:
		/// 400: missing data
		/// 401: incorrect login info
		/// 409: Name/Email was already used. See return for which one it was
		/// </summary>
		private string EditAcc(HttpListenerRequest request, HttpListenerResponse response) {
			var sid = request.QueryString.Get("sid");
			var pw = request.QueryString.Get("password");
			if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(pw)) {
				response.StatusCode = 400;
				return "";
			}
			var s = DB.FindSession(request.QueryString.Get("sid"));
			var UID = s?.UserId ?? -1;
			if (s == null || UID < 0) {
				response.StatusCode = 401;
				return "";
			}
			var usr = DB.FindUser(UID);
			if (usr?.Password != pw) {
				response.StatusCode = 401;
				return "";
			}
			if (request.QueryString.Count == 2)
				return Json.Encode(new User() { Username = usr.Username, Email = usr.Email });
			else {
				var npw = request.QueryString.Get("newPassword");
				if (!string.IsNullOrWhiteSpace(npw))
					usr.Password = npw;
				var nun = request.QueryString.Get("newUsername");
				if (!string.IsNullOrWhiteSpace(nun)) {
					if (DB.FindUser(nun) == null)
						usr.Username = nun;
					else {
						response.StatusCode = 409;
						return "" + (char)RegisterErrorState.NameFound;
					}
				}
				var nem = request.QueryString.Get("email");
				if (!string.IsNullOrWhiteSpace(nem)) {
					if (DB.FindEmail(nem) == null)
						usr.Email = nem;
					else {
						response.StatusCode = 409;
						return "" + (char)RegisterErrorState.EmailFound;
					}
				}
				DB.UpdateUser(usr);
			}
			return "";
		}
		/// <summary>
		/// Handles the Delete account page.
		/// Ensures that the SID and password are valid, then proceeds to send the user a email.
		/// </summary>
		private string DelAcc(HttpListenerRequest request, HttpListenerResponse response) {
			var sid = request.QueryString.Get("sid");
			var pw = request.QueryString.Get("password");
			if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(pw)) {
				response.StatusCode = 400;
				return "";
			}
			var s = DB.FindSession(request.QueryString.Get("sid"));
			var UID = s?.UserId ?? -1;
			if (s == null || UID == -1) {
				response.StatusCode = 401;
				return "";
			}
			var usr = DB.FindUser(UID);
			if (usr.Password != pw) {
				response.StatusCode = 401;
				return "";
			}
			DB.SndDelteEmail(usr.Email, this);
			return "";
		}
		/// <summary>
		/// Handles the wait log creation page.
		/// Ensures that all information is valid, then creates a new waitlog.
		/// </summary>
		private string Wait(HttpListenerRequest request, HttpListenerResponse response) {
			var targetID = request.QueryString.Get("target");
			var session = DB.FindSession(request.QueryString.Get("sid"));
			if (targetID == null || session == null || !int.TryParse(targetID, out var ID) || session.IsExpired())
				response.StatusCode = 401;
			else {
				if (!DB.FindUser(ID, out var target))
					response.StatusCode = 404;
				else
					ServerFrameWork.QUWI("Wait", () => DB.CreateNewWaitLog(DB.FindUser(session.UserId), target));
			}
			return "";
		}
		/// <summary>
		/// Handles the Me page.
		/// Returns general information regarding the current user as JSON
		/// </summary>
		private string Me(HttpListenerRequest request, HttpListenerResponse response) {
			if (!ValidateLogin(request.QueryString, out var UID)) {
				response.StatusCode = 401;
				return "";
			}
			var sb = DB.FindUser(UID);
			sb.Password = null;
			response.ContentType = "application/json";
			return Json.Encode(sb);
		}
		/// <summary>
		/// Handles the Me page.
		/// Sends a restore email if the given email address has been found
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		private string Restore(HttpListenerRequest request, HttpListenerResponse response) {
			var email = request.QueryString.Get("email");
			if (string.IsNullOrEmpty(email)) {
				response.StatusCode = 400;
				return "";
			}
			DB.RestoreAccount(email, this);
			return "";
		}
		/// <summary>
		/// Handles the Password Reset page.
		/// This is the actual restore website visible for browsers.
		/// Shows the newly generated Password and the associated username
		/// </summary>
		private string EmailValidation(HttpListenerRequest request, HttpListenerResponse response) =>
			((Func<Tuple<string, string>, string>)(
				(rest) =>
					Html.BodyBuilder(Html.h1("Punkte In Wedding Account Wiederherstellung") + (
						rest == null ?
						   Html.b("Dieser Link ist Ungültig oder Abgelaufen.") + Html.br +
						   Html.txt("Wenn sie einen Account wiederherstellen wollen, fordern sie den Link erneut an")
						:
						   Html.h2("Account Erfolgreich Wiederhergestellt!") +
						   Html.h3("Das sind ihre neuen Zugangsdaten:") +
						   Html.b("Username:") + Html.txt($" {rest.Item1}{Html.br}") +
						   Html.b("Passwort:") + $" {rest.Item2}{Html.br}" +
						   Html.txt("Sie können das Passwort dann in der App ändern")
						)
					)
				)
			)(DB.DoEmailRestore(request.QueryString.Get("id"), request.QueryString.Get("valid")));
		/// <summary>
		/// Handles the Account deletion page.
		/// Shows whether the account has been deleted or if the deletion has been aborted
		/// </summary>
		private string DeleteValidation(HttpListenerRequest request, HttpListenerResponse response) => ((Func<bool?, string>)((d) => Html.BodyBuilder(Html.h1("Punkte In Wedding Account Löschung") + d == null ? Html.p(Html.b("Dieser Link ist Ungültig oder Abgelaufen.")) + Html.txt("Wenn sie einen Account löschen wollen, fordern sie den Link erneut an") : d == true ? Html.h2("Account Löschung Erfolgreich!") : Html.h2("Account Löschung Abgebrochen"))))(DB.DoEmailDelete(request.QueryString.Get("id"), request.QueryString.Get("valid"), request.QueryString.Get("action")));
		/// <summary>
		///  Handles the welcome page. 
		///  Shows some debug info
		/// </summary>
		private string WelcomePage(HttpListenerRequest request, HttpListenerResponse response) =>
			Html.BodyBuilder(Html.h1("Punkte in Wedding Server") +
			Html.p(Html.b("This is your user Agent") + Html.txt(ServerFrameWork.RecursiveToString(request.UserAgent))) +
			Html.p(Html.b("This is your URL:") + Html.txt(ServerFrameWork.RecursiveToString(request.Url))) +
			Html.p(Html.b("This is your QueryInfo:") + Html.txt(ServerFrameWork.RecursiveToString(request.QueryString))) +
			Html.p(Html.b("Which means:") + Html.txt(ServerFrameWork.ReadQuery(request.QueryString))) +
			Html.p(Html.b("This is your Endpoint:") + Html.txt(ServerFrameWork.RecursiveToString(request.RemoteEndPoint))) +
			Html.p(Html.b("Which means your IP is:") + Html.txt($"{request.RemoteEndPoint.Address} and you came from port {request.RemoteEndPoint.Port}"))) +
			Html.br + Html.h2("Now Feck off!") +
			Html.txt("by the way, this info is not stored in any way");
		/// <summary>
		/// Handles the Recent Activity page.
		/// Returns a JSON formatted list of <see cref="TinyLog"/>s.
		/// These are all instances where others have waited on the current user
		/// </summary>
		private string RecentActivity(HttpListenerRequest request, HttpListenerResponse response) {
			if (!ValidateLogin(request.QueryString, out var UID)) {
				response.StatusCode = 401;
				return "";
			}
			response.ContentType = "application/json";
			return Json.Encode(DB.GetRecentActivity(UID));
		}
		/// <summary>
		/// Stops the server and cleans up the database
		/// </summary>
		public void Stop() {
		}
	}
}
