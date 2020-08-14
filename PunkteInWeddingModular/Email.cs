using MailKit.Net.Smtp;
using MimeKit;
using System.IO;
namespace System {
	/*
	 * EmailTokens.txt structure:
	 * [email]
	 * [Obfuscated char 1],[obfuscated char 2],[obfuscated char ...]
	 * [smtp server]
	 * [port]
	 */
	 /// <summary>
	 /// Email Client using the <see cref="SmtpClient"/>
	 /// </summary>
	static class Email {
		/// <summary>
		/// Or email client
		/// </summary>
		static readonly SmtpClient client = new SmtpClient();
		/// <summary>
		/// De-obfuscation of the email password
		/// </summary>
		private static string Pw {
			get {
				var a = new Collections.Generic.List<int>();
				for (int b = 'A'; b <= 'Z';) a.Add(b++);
				for (int b = '!'; b <= '/';) a.Add(b++);
				for (int b = 'a'; b <= '~';) a.Add(b++);
				for (var b = 'Z' + 1; b < 'a';) a.Add(b++);
				for (int b = '!'; b < '?';) a.Add(b++);
				var c = File.ReadAllLines("EmailTokens.txt")[1].Split(',');
				var d = "";
				foreach (var b in c)
					d += (char)a[(int)Math.Round(Math.Pow(int.Parse(b), 1.0 / 3.0))];
				return d;
			}
		}
		static string email;
		/// <summary>
		/// Connect to the email service and authenticate
		/// </summary>
		static void ConnectAndAuth() {
			var lines = File.ReadAllLines("EmailTokens.txt");
			client.Connect(lines[2], int.Parse(lines[3]), true);
			email=lines[0];
			client.Authenticate(email, Pw);
		}
		/// <summary>
		/// Send a given email
		/// </summary>
		/// <param name="me"></param>
		static public void Send(MimeMessage me) {
			if (!client.IsAuthenticated)
				ConnectAndAuth();
			client.Send(me);
		}
		/// <summary>
		/// Restore email message template
		/// </summary>
		static readonly string[] restoreEmail = new string[]{
			"Punkte In Wedding Account Wiederherstellung",
			"Hallo,",
			"Du hast gerade eine Account Wiederherstellung beantragt.",
			"Wenn du den folgenden Link klickst, erhältst du neue Zugangsdaten zu deinem Konto.",
			"Achtung! Dein Passwort wird dabei automatisch geändert.",
			"Ändere das Passwort danach in der App auf etwas individuelles",
			"Der Link ist für 24h gültig.",
			"Wenn du die Wiederherstellung nicht beantragt hast, kannst du diese Email ignorieren"
		};
		/// <summary>
		/// Delete email message template
		/// </summary>
		static readonly string[] deleteEmail = new string[] {
			"Punkte In Wedding Account Löschung",
			"Hallo, ",
			"Du hast gerade eine Account Löschung beantragt.",
			"Wenn du den folgenden Link klickst, wird dein Konto und alle dazugehörige Daten gelöscht.",
			"Achtung! Es können keinerlei Daten wiederhergestellt werden",
			"Wenn du dir sicher bist dsas du den Account löschen willst, klicke den Link:",
			"Der Link ist für 1h gültig.",
			"Wenn du die Löschung nicht beantragt hast, kannst du diese Email ignorieren oder den folgenden Link zur deaktiverung der Löschung anklicken:"
		};
		/// <summary>
		/// Generate a email with the given <paramref name="restoreURL"/> and send it to the recipient.
		/// </summary>
		/// <param name="restoreURL">URL for the user to click</param>
		/// <param name="to">Email address of the recipient</param>
		public static void SendRestoreKey(string restoreURL, string to) {
			if (!client.IsAuthenticated)
				ConnectAndAuth();
			var msg = new MimeMessage {
				Body = new BodyBuilder {
					TextBody =
						$"{restoreEmail[1]}\n{restoreEmail[2]}\n" +
						$"{restoreEmail[3]}\n" +
						$"{restoreEmail[4]}\n" +
						$"{restoreEmail[5]}\n" +
						$" {restoreURL} \n" +
						$"{restoreEmail[6]}\n" + restoreEmail[7],
					HtmlBody =
						Html.BodyBuilder(
							Html.h1(restoreEmail[0]) +
							Html.p(restoreEmail[1] + Html.br + restoreEmail[2]) +
							Html.txt(restoreEmail[3]) + Html.br +
							Html.b(restoreEmail[4]) + Html.br +
							Html.txt(restoreEmail[5]) + Html.br +
							Html.a(restoreURL, restoreURL) + Html.br +
							Html.txt(restoreEmail[6]) +
							Html.h2(restoreEmail[7])
						)
				}.ToMessageBody(),
				Subject = restoreEmail[0]
			};
			msg.From.Add(new MailboxAddress("DVSProductions Support", email));
			msg.To.Add(new MailboxAddress(to, to));
			Send(msg);
		}
		/// <summary>
		/// Generate a email with the given <paramref name="restoreURL"/> and send it to the recipient.
		/// </summary>
		/// <param name="delUrl">URL for the user to click if they wish to delete their account</param>
		/// <param name="stopDelUrl">URL for the user to click if they wish to keep their account</param>
		/// <param name="to">Email address of the recipient</param>
		public static void SendDeleteEmail(string delUrl, string stopDelUrl, string to) {
			if (!client.IsAuthenticated)
				ConnectAndAuth();
			var msg = new MimeMessage {
				Body = new BodyBuilder {
					TextBody =
						$"{deleteEmail[1]}\n{deleteEmail[2]}\n" +
						$"{deleteEmail[3]}\n" +
						$"{deleteEmail[4]}\n" +
						$"{deleteEmail[5]}\n" +
						$"{delUrl}\n" +
						$"{deleteEmail[6]}\n" +
						$"{deleteEmail[7]}\n" +
						$"{stopDelUrl}\n",
					HtmlBody =
						Html.BodyBuilder(
							Html.h1(deleteEmail[0]) +
							Html.txt(deleteEmail[1]) + Html.br +
							Html.txt(deleteEmail[2]) + Html.br +
							Html.txt(deleteEmail[3]) + Html.br +
							Html.b(deleteEmail[4]) + Html.br +
							Html.txt(deleteEmail[5]) + Html.br +
							Html.a(delUrl, delUrl) + Html.br +
							Html.txt(deleteEmail[6]) +
							Html.h2(deleteEmail[7]) +
							Html.a(stopDelUrl, stopDelUrl)
						)
				}.ToMessageBody(),
				Subject = deleteEmail[0]
			};
			msg.From.Add(new MailboxAddress("DVSProductions Support", email));
			msg.To.Add(new MailboxAddress(to, to));
			Send(msg);
		}
	}
}
