using System;
using System.Collections.Generic;
using System.Net;
namespace PunkteInWeddingModular {
	public partial class PIW {
		/// <summary>
		/// This command allows administrators in the console to view data in the databases
		/// </summary>
		private class ListCommand : ICommand {
			public string Verb => "list";
			/// <summary>
			/// Parent Instance for access
			/// </summary>
			readonly PIW parent;
			public string AssignedName { get; set; }
			public ListCommand(PIW p) => parent = p;
			/// <summary>
			/// Possible objects to print
			/// </summary>
			enum Objs {
				users, logs, restores
			}
			private void PrintLogs(List<string> parameters) {
				if (parameters.Count < 5) {
					var logs = new List<Logentry>(parent.DB.LogList);
					int count = 10, offset = 0, sidx = 1;
					bool decode = false, ignore = false; ;
					if (parameters.Count >= 2) {
						if (parameters[sidx].ToLower() == "-d") {
							sidx++;
							decode = true;
						}
						else if (parameters[sidx].ToLower() == "-di") {
							sidx++;
							decode = true;
							ignore = true;
						}
					}
					if (parameters.Count >= sidx + 1 && !int.TryParse(parameters[sidx], out count)) {
						C.WriteLineE($"{parameters[sidx]} isn't a Number");
						return;
					}
					if (parameters.Count == sidx + 2 && !int.TryParse(parameters[sidx + 1], out offset)) {
						C.WriteLineE($"{parameters[sidx + 1]} isn't a Number");
						return;
					}
					if (parameters.Count > sidx + 2) {
						C.WriteLineE("Too many parameters");
						return;
					}
					var max = Math.Min(count + offset, logs.Count);
					var chars = ("" + (max + 1)).Length;
					string GUN(int id) => parent.DB.FindUser(id)?.Username ?? "[deleted]";
					string decodeL(Logentry l) => $"[{l.When.ToShortDateString()} {l.When.ToShortTimeString()}]: {GUN(l.Waitee)} waited on {GUN(l.WatingOn)}";
					for (var n = offset; n < max; n++) {
						var logstr = decode ? decodeL(logs[n]) : logs[n].ToString();
						if (ignore && logstr.Contains("[deleted]")) {
							count++;
							max = Math.Min(count + offset, logs.Count);
							chars = ("" + max).Length;
							continue;
						}
						C.WriteLine(string.Format($"{{0:D{chars}}}: {logstr}", logs[n].Id));
					}
				}
				else C.WriteLineE("Too many parameters");
			}
			private void PrintUsers(List<string> parameters) {
				if (parameters.Count > 2) {
					C.WriteLineE("too many parameters");
					return;
				}
				var showEmail = false;
				if (parameters.Count == 2) {
					if (parameters[1].ToLower() != "-e") {
						C.WriteLineE($"Unrecognized parameter \"{parameters[1]}\"");
						return;
					}
					else showEmail = true;
				}
				var lst = parent.DB.UserList;
				foreach (var u in lst)
					C.WriteLine($"{u.Id}: {u.Username}{(showEmail ? $" email: {u.Email}" : "")}");
			}

			private void PrintRestores(List<string> parameters) {
				if (parameters.Count < 2) {
					var l = parent.DB.SessionList;
					foreach (var s in l) {
						if (s.UserId < 0)
							C.WriteLine($"{s.Id}: Restore email for {parent.DB.FindUser(-s.UserId).Username} on {s.Created}");
					}
				}
			}
			public void Execute(List<string> parameters) {
				if (parameters.Count == 0) {
					C.WriteLineI($"Invalid parameter count. see help {AssignedName} for more details");
					return;
				}
				if (Enum.TryParse(parameters[0].ToLower(), out Objs selected)) {
					switch (selected) {
						case Objs.users:
							PrintUsers(parameters);
							return;
						case Objs.logs:
							PrintLogs(parameters);
							return;
						case Objs.restores:
							PrintRestores(parameters);
							return;
						default:
							C.WriteLineI($"Unkown parameter. Use help {AssignedName} to see valid options");
							return;
					}
				}
				else {
					C.WriteLineI($"Unkown parameter. Use help {AssignedName} to see valid options");
				}
			}
			public void Help(List<string> parameters) {
				if (parameters.Count == 0) {
					C.WriteLine("Prints any of the given objects:");
					foreach (var e in Enum.GetValues(typeof(Objs)))
						C.WriteLine($"\t{e}");
					C.WriteLine($"Usage: {AssignedName} [obj]");
					C.WriteLine($"Example: {AssignedName} {Enum.GetValues(typeof(Objs)).GetValue(0)}");
				}
				else if (parameters.Count == 1) {
					if (Enum.TryParse(parameters[0].ToLower(), out Objs selected)) {
						switch (selected) {
							case Objs.users:
								C.WriteLine("Lists all users");
								C.WriteLine($"Usage: {AssignedName} users [-e]");
								C.WriteLine("\t -e   Shows Email adresses of users");
								return;
							case Objs.logs:
								C.WriteLine($"Prints the 10 latest logfiles equivalent to {AssignedName} logs 10 0");
								C.WriteLine($"Usage: {AssignedName} logs [-d[I]] [count] [offset]");
								C.WriteLine("\t -d:     when is flag is set ids will be decoded");
								C.WriteLine("\t -dI:    ids will be decoded and deleted will be ignored");
								C.WriteLine("\t count:  determines the amount of logs to print");
								C.WriteLine("\t offset: determines at which log num to start");
								C.WriteLine($"Example: \"{AssignedName} logs 5 10\"  will print 5 logs starting from the 10th log ");
								return;
							case Objs.restores:
								C.WriteLine("Shows all sent restore emails");
								return;
							default:
								C.WriteLine("Whoops, someone forgot to implement a message here");
								return;
						}
					}
					else {
						C.WriteLine($"Unrecognized parameter {parameters[0]}");
						C.WriteLine("Try one of these:");
						foreach (var e in Enum.GetValues(typeof(Objs)))
							C.WriteLine($"\t{e}");
					}
				}
			}
		}
	}
}
