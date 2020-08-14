using LiteDB;
using PunkteInWedding.Models;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace System {
	/// <summary>
	/// Database interface using <see cref="LiteDatabase"/>
	/// </summary>
	public partial class Database : IDisposable {
		/// <summary>
		/// Indicates that everything should come to a stop immediately.
		/// While <see langword="true"/> Nothing should be written or read from the database
		/// </summary>
		private bool DEADLOCK = false;
		/// <summary>
		/// Cryptographically secure random generator for all your random needs
		/// </summary>
		readonly RNGCryptoServiceProvider rng;
		/// <summary>
		/// Source database
		/// </summary>
		readonly LiteDatabase db;
		/// <summary>
		/// Collection of all current users
		/// </summary>
		readonly ILiteCollection<User> users;
		/// <summary>
		/// Collection of all current log entries
		/// </summary>
		readonly ILiteCollection<Logentry> log;
		/// <summary>
		/// Collection of all current session IDs
		/// </summary>
		readonly ILiteCollection<SessionID> sids;
		/// <summary>
		/// Current scoreboard
		/// </summary>
		readonly SwitchableManagement<List<User>> Scoreboard;
		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="User"/>s with the UserID as Key.
		/// Used for very fast user identification
		/// </summary>
		readonly SwitchableManagement<Dictionary<int, User>> TurboMap;
		/// <summary>
		/// Lists all Sessions without SIDs (for security)
		/// </summary>
		public List<SessionID> SessionList {
			get {
				var ret = new List<SessionID>(sids.FindAll());
				foreach (var s in ret) s.SID = null;//protect malicious SID attacks
				return ret;
			}
		}
		/// <summary>
		/// Returns the current list of all users (using <see cref="SwitchableManagement{T}"/>)
		/// </summary>
		public IEnumerable<User> UserList => TurboMap.Get.Values;
		/// <summary>
		/// Returns all Logs
		/// </summary>
		public IEnumerable<Logentry> LogList => log.FindAll();
		/// <summary>
		/// Wait for deadlock, then continue
		/// </summary>
		void WaitforDeadLock() {
			while (DEADLOCK) Threading.Tasks.Task.Delay(1000).Wait();
		}
		/// <summary>
		/// Wait for deadlock, then execute the <see cref="Func{TResult}"/> and return its value
		/// </summary>
		/// <param name="f">Function to execute</param>
		/// <returns></returns>
		T WaitforDeadLock<T>(Func<T> f) {
			while (DEADLOCK) Threading.Tasks.Task.Delay(1000).Wait();
			return f();
		}
		/// <summary>
		/// Implements the <see cref="SwitchableManagement{T}.Updater"/> delegate to generate the <see cref="Scoreboard"/>
		/// </summary>
		void ScoreboardUpdater(ref List<User> l) {
			l?.Clear();
			l = new List<User>(users.FindAll());
			l.ForEach((e) => { e.Password = null; e.Email = null; });
			l.RemoveAll((u) => string.IsNullOrWhiteSpace(u.Username));
			l.Sort();
		}
		/// <summary>
		/// Implements the <see cref="SwitchableManagement{T}.Updater"/> delegate to generate the <see cref="TurboMap"/>
		/// </summary>
		void TurboUpdater(ref Dictionary<int, User> l) {
			l?.Clear();
			l = new Dictionary<int, User>();
			foreach (var u in users.FindAll())
				if (!string.IsNullOrEmpty(u.Username))
					l.Add(u.Id, u.Clone());
		}
		/// <summary>
		/// Create a Instance of the Database Class
		/// </summary>
		public Database() {
			db = new LiteDatabase("PunkteInWedding.db");
			users = db.GetCollection<User>("users");
			log = db.GetCollection<Logentry>("log");
			sids = db.GetCollection<SessionID>("sids");
			rng = new RNGCryptoServiceProvider();
			Scoreboard = new SwitchableManagement<List<User>>(new List<User>(), new List<User>(), ScoreboardUpdater);
			TurboMap = new SwitchableManagement<Dictionary<int, User>>(new Dictionary<int, User>(), new Dictionary<int, User>(), TurboUpdater);
			Scoreboard.DoRegularUpdates(new TimeSpan(0, 5, 0));
			TurboMap.DoRegularUpdates(new TimeSpan(5, 0, 0));
		}
		/// <summary>
		/// Ensures that a given uses is actually who they claim they are
		/// </summary>
		/// <param name="u">User to validate</param>
		/// <returns>true if the user has been found and their password is valid.</returns>
		public bool VerifyUser(User u) => WaitforDeadLock(() => FindUser(u.Username)?.Password == u.Password);
		/// <summary>
		/// Finds and returns a user based on their Username.
		/// If the user has not been found this function will return <see langword="null"/>
		/// </summary>
		public User FindUser(string username) {
			if (string.IsNullOrEmpty(username))
				return null;
			var name = username.ToLower();
			WaitforDeadLock();
			return users.FindOne((u) => u.Username != null && name == u.Username.ToLower());
		}
		/// <summary>
		/// Find a user using their ID.
		/// Uses Direct Database access. Safer but lowers performance.
		/// </summary>
		/// <param name="id">ID of the target user</param>
		public User FindUserSafe(int id) => WaitforDeadLock(() => users.FindById(id));
		/// <summary>
		/// Find a user using their ID.
		/// Uses <see cref="SwitchableManagement{T}"/>. Super fast but a user may be not be in the list yet.
		/// </summary>
		/// <param name="id">ID of the target user</param>
		public User FindUser(int id) => TurboMap.Get.ContainsKey(id) ? TurboMap.Get[id].Clone() : null;
		/// <summary>
		/// Find a user using their ID.
		/// Uses <see cref="SwitchableManagement{T}"/>. Super fast but a user may be not be in the list yet.
		/// Returns whether the user was found for much easier validation
		/// </summary>
		/// <param name="id">ID of the target user</param>
		/// <param name="u">Where to store the found user</param>
		public bool FindUser(int id, out User u) => TurboMap.Get.TryGetValue(id, out u);
		/// <summary>
		/// Finds a User by their email address
		/// </summary>
		/// <param name="email">email to find</param>
		public User FindEmail(string email) {
			if (email == null)
				return null;
			var e = email.ToLower();
			WaitforDeadLock();
			return users.FindOne((u) => u.Email != null && u.Email.ToLower() == e);
		}
		/// <summary>
		/// Locates a <see cref="SessionID"/> using its ID
		/// </summary>
		/// <param name="SID">SID of the session</param>
		public SessionID FindSession(string SID) => string.IsNullOrWhiteSpace(SID) ? null : FindAndExtendSession(SID);
		/// <summary>
		/// Finds a <see cref="SessionID"/> by its ID. 
		/// Also automatically extends its lifetime so it doesn't die.
		/// This ensures that sessions that are in use never die randomly.
		/// </summary>
		/// <param name="SID">SID of the session</param>
		private SessionID FindAndExtendSession(string SID) {
			WaitforDeadLock();
			var s = sids.FindOne((e) => e.SID == SID);
			if (s?.Id >= 0 && s?.ExtendLiftetime() == true)
				sids.Update(s);
			return s;
		}
		/// <summary>
		/// Attempt to log a user in. 
		/// This will validate whether all credentials are valid an then generate a SID
		/// </summary>
		/// <param name="usr">Source User information</param>
		/// <param name="SID">Output Session id</param>
		/// <returns>Whether the user has been logged in successfully</returns>
		public bool Login(User usr, out string SID) {
			SID = null;
			if (usr == null) return false;
			WaitforDeadLock();
			var e = FindUser(usr.Username);//finde den User
			if (e == null || usr.Password != e.Password) {//wenn er nicht gefunden wurde ende
				return false;
			}
			SessionID sid;//erzeuge neue Session
			do sid = new SessionID(e.Id, rng); while (FindSession(sid.SID) != null);
			sids.Insert(sid);
			SID = sid.SID;//gebe sie aus
			C.WriteLineI("Logged In: " + usr.Username);
			return true;
		}
		/// <summary>
		/// Registers a new user.
		/// This will ensure that the account details are unique and then generate a new Account.
		/// Returns error details or success
		/// </summary>
		/// <param name="u">User information</param>
		/// <returns></returns>
		public RegisterErrorState Register(User u) {
			if (u == null) return RegisterErrorState.Fail;
			WaitforDeadLock();
			u.Email = u.Email.ToLower();
			if (FindUser(u.Username) != null)
				return RegisterErrorState.NameFound;
			else if (FindEmail(u.Email) != null)
				return RegisterErrorState.EmailFound;
			users.Insert(u);
			C.WriteLineS("Registered " + u.Username);
			Scoreboard.Update();
			TurboMap.Update();
			return RegisterErrorState.Success;
		}
		/// <summary>
		/// Counts the number of users currently registered
		/// </summary>
		public long Count => users.LongCount();
		/// <summary>
		/// Returns the Scoreboard. 
		/// </summary>
		public List<User> GetScoreboard() => Scoreboard.Get;
		/// <summary>
		/// Returns who and when people waited on this user ID
		/// </summary>
		/// <param name="UID">User id of the person that was waited on</param>
		public List<TinyLog> GetRecentActivity(int UID) {
			WaitforDeadLock();
			var l = new List<Logentry>(log.Find((a) => a.WatingOn == UID));
			var ret = new List<TinyLog>(l.Count);
			foreach (var e in l)
				ret.Add(e.GetTinyLog(TurboMap.Get[e.Waitee].Username));
			return ret;
		}
		/// <summary>
		/// Generates a Wait log where the <paramref name="waitee"/> waits on the <paramref name="target"/>
		/// </summary>
		/// <param name="waitee">User that is waiting</param>
		/// <param name="target">Subject that is beeing waited on</param>
		public void CreateNewWaitLog(User waitee, User target) {
			if (waitee == null || target == null) return;
			WaitforDeadLock();
			log.Insert(new Logentry() {
				WatingOn = target.Id,
				Waitee = waitee.Id,
				When = DateTime.Now
			});
			waitee.Score--;
			target.Score++;
			users.Update(waitee);
			users.Update(target);
			C.WriteLine($"{waitee.Username} is waiting on {target.Username}");
			Scoreboard.Update();
		}
		/// <summary>
		/// Makes a Session Invalid
		/// </summary>
		/// <param name="SID">SessionID of the session to invalidate</param>
		public void InvalidateSession(string SID) {
			if (string.IsNullOrWhiteSpace(SID))
				return;
			var sesh = FindSession(SID);
			if (sesh == null)
				return;
			sesh.DeathOn = DateTime.Now;
			sids.Update(sesh);
		}
		/// <summary>
		/// Sends a restore Email to a user. 
		/// Runs <see langword="async"/> on a different thread, so this method exits instantly
		/// </summary>
		/// <param name="email">Email of the user to send the restore to</param>
		/// <param name="parent">Calling <see cref="IServer"/>(used for acquiring the URL)</param>
		public void RestoreAccount(string email, IServer parent) => ServerFrameWork.QUWI("DB::RestoreAccount", () => {
			WaitforDeadLock();
			var u = FindEmail(email);
			if (u == null)
				return;
			var fakeSess = new SessionID(-u.Id, rng) { DeathOn = DateTime.Now + new TimeSpan(1, 0, 0, 0) };
			try {
				Email.SendRestoreKey($"{ServerInfo.CreateURL.ToTStorage()(parent)}EmailValidation?id={u.Id}&valid={fakeSess.SID}", email);
				sids.Insert(fakeSess);
			}
			catch { }
		});
		/// <summary>
		/// Action options. Used in the deletion email to allow the user to cancel this request
		/// </summary>
		const string a_delete = "delete", a_cancel = "cancel";
		/// <summary>
		/// Send the deletion request email.
		/// </summary>
		/// <param name="email">Email of the user to send the verification E-mail to</param>
		/// <param name="parent">Calling <see cref="IServer"/>(used for acquiring the URL)</param>
		public void SndDelteEmail(string email, IServer parent) => ServerFrameWork.QUWI("DB::SndDelteEmail", () => {
			WaitforDeadLock();
			var u = FindEmail(email);
			if (u == null)
				return;
			var fakeSess = new SessionID(-u.Id, rng) { DeathOn = DateTime.Now + new TimeSpan(1, 0, 0) };
			try {
				string gen(string a) => $"{ServerInfo.CreateURL.ToTStorage()(parent)}EmailValidation/Delete?action={a}&id={u.Id}&valid={fakeSess.SID}";
				Email.SendDeleteEmail(gen(a_delete), gen(a_cancel), email);
				sids.Insert(fakeSess);
			}
			catch { }
		});
		/// <summary>
		/// Generated Password Characters, Easy to remember, no caps
		/// </summary>
		const string charset = "abcdefghijklmnopqrstuvwxyz12345679";
		/// <summary>
		/// Cryptographic Password Generation
		/// </summary>
		/// <param name="length">Password length</param>
		/// <returns>New Password</returns>
		public string GenPW(int length) {
			var b = new byte[length];
			rng.GetNonZeroBytes(b);
			var s = "";
			foreach (var c in b)
				s += charset[c % charset.Length];
			return s;
		}
		/// <summary>
		/// Restore an account from the restore email.
		/// This resets the password to one we generate and show on the website.
		/// This allows the user to log in with the temporary password and then change it.
		/// </summary>
		/// <param name="UID">ID of the user in question</param>
		/// <param name="SID">ID of the fake session created for this occasion</param>
		public Tuple<string, string> DoEmailRestore(string UID, string SID) {
			WaitforDeadLock();
			if (!int.TryParse(UID, out var uid))
				return null;
			var u = FindUser(uid);
			if (u == null)
				return null;
			var sesh = FindSession(SID);
			if (sesh == null || sesh.UserId != -uid || sesh.IsExpired())
				return null;
			sesh.DeathOn = DateTime.Now;
			var newPW = GenPW(10);
			u.Password = Encryption.EncryptPassword(u.Username.ToLower(), newPW);
			sids.Update(sesh);
			users.Update(u);
			TurboMap.Update();
			C.WriteLineI($"Restored account for: {u.Username}");
			return new Tuple<string, string>(u.Username, newPW);
		}
		/// <summary>
		/// Deletes or cancels the deletion of a given account using their UID and the fake session ID
		/// Returns <see langword="null"/> if any of the information is invalid.
		/// Returns <see langword="true"/> if the Account has been deleted.
		/// Returns <see langword="false"/> if the Deletion has been canceled
		/// </summary>
		/// <param name="UID">User id to terminate</param>
		/// <param name="SID">Fake session ID</param>
		/// <param name="action">Either <see cref="a_cancel"/> or <see cref="a_delete"/></param>
		/// <returns></returns>
		public bool? DoEmailDelete(string UID, string SID, string action) {
			WaitforDeadLock();
			if (!int.TryParse(UID, out var uid))
				return null;
			var u = FindUser(uid);
			if (u == null)
				return null;
			var sesh = FindSession(SID);
			if (sesh == null || sesh.UserId != -uid || sesh.IsExpired())
				return null;
			sesh.DeathOn = DateTime.Now;
			sids.Update(sesh);
			if (action != a_delete)
				return false;
			u.Password = "";
			u.Score = 0;
			u.Email = "";
			var n = u.Username;
			u.Username = "";
			users.Update(u);
			sids.DeleteMany((s) => s.UserId == u.Id);
			C.WriteLineE($"Deleted Account for: {n}");
			Scoreboard.Update();
			TurboMap.Update();
			return true;
		}
		/// <summary>
		/// Update the information about a given user.
		/// Runs on a different thread and therefor returns instantly
		/// </summary>
		/// <param name="u">User to update</param>
		public void UpdateUser(User u) => ServerFrameWork.QUWI("UpdateUser", (o) => {
			var us = o as User;
			WaitforDeadLock();
			users.Update(us);
			TurboMap.Update();
			Scoreboard.Update();
			C.WriteLine($"Updated: {us.Username}");
		}, u);
		/// <summary>
		/// Initiate a full trace of all logs and scores.
		/// Re-evaluates all wait logs and determines the users final scores
		/// </summary>
		public void Fulltrace() {
			DEADLOCK = true;
			C.WriteLine("Waiting a bit to allow pending operations to complete");
			Threading.Tasks.Task.Delay(5000).Wait();
			C.WriteLine("Starting Trace");
			var s = Diagnostics.Stopwatch.StartNew();
			var udict = new Dictionary<int, User>();
			foreach (var u in users.FindAll()) {
				u.Score = 0;
				udict.Add(u.Id, u);
			}
			var count = 0;
			foreach (var l in log.FindAll()) {
				try {
					udict[l.Waitee].Score--;
					udict[l.WatingOn].Score++;
					count++;
				}
				catch { }
			}
			foreach (var u in udict.Values)
				users.Update(u);
			Scoreboard.Update();
			TurboMap.Update();
			C.WriteLine("Trace complete. Waiting for Updates");
			while (Scoreboard.IsBusy) Threading.Tasks.Task.Delay(250).Wait();
			while (TurboMap.IsBusy) Threading.Tasks.Task.Delay(250).Wait();
			s.Stop();
			C.WriteLineS($"Full trace completed in {(s.ElapsedMilliseconds < 1000 ? $"{s.ElapsedMilliseconds}m" : $"{s.ElapsedMilliseconds / 1000}")}s");
			C.WriteLine($"Processed {count} logs");
			DEADLOCK = false;
		}
		private bool isDisposed;
		protected virtual void Dispose(bool disposing) {
			if (isDisposed) return;
			if (disposing) {
				db.Dispose();
				rng.Dispose();
			}
			isDisposed = true;
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
