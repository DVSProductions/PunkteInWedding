using PunkteInWedding.Models;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RegisterPage : ContentPage {

		Action close;
		private User user;
		static string RmSpace(string s) => s == null ? "" : s.TrimEnd(' ').TrimStart(' ');
		void GenUser() => user = WebHandler.CreateUser(RmSpace(eUsername.Text), RmSpace(eEmail.Text), RmSpace(ep.Text));
		public RegisterPage(Action close) {
			this.close = close;
			InitializeComponent();
			UpdatePreview();
		}

		static Color Bad => Color.FromRgba(1, 0, 0, 0.5);
		static Color Meh => Color.FromRgba(1, 1, 0, 0.5);
		static Color Good => Color.Transparent;
		bool Validate() {
			if (string.IsNullOrWhiteSpace(ep.Text)) {
				ep.BackgroundColor = Bad;
				return false;
			}
			else
				ep.BackgroundColor = Good;
			if (string.IsNullOrWhiteSpace(epa.Text)) {
				epa.BackgroundColor = Bad;
				return false;
			}
			else
				epa.BackgroundColor = Good;
			if (ep.Text != epa.Text) {
				epa.BackgroundColor = ep.BackgroundColor = Bad;
				return false;
			}
			else if (ep.Text.Length < 5) {
				ep.BackgroundColor = Meh;
				DisplayAlert("Warnung", "Passwörter müssen mindestens 5 Zeichen lang sein!", "aww");
				return false;
			}
			else {
				epa.BackgroundColor = ep.BackgroundColor = Good;
			}
			GenUser();
			if (string.IsNullOrWhiteSpace(user.Email) || !WebHandler.IsValidEmail(user.Email)) {
				eEmail.BackgroundColor = Bad;
				return false;
			}
			else
				eEmail.BackgroundColor = Good;
			if (string.IsNullOrWhiteSpace(user.Username) || user.Username.Contains("+") || user.Username.Contains("&")) {
				eUsername.BackgroundColor = Bad;
				return false;
			}
			else if (user.Username.Length > 15 || user.Username.Length < 4) {
				eUsername.BackgroundColor = Meh;
				DisplayAlert("Warnung", "Nutzernamen müssen mindestens 4 und maximal 15 Zeichen lang sein!", "aww");
				return false;
			}
			else
				eUsername.BackgroundColor = Good;
			return true;
		}
		async void Register_Clicked(object sender, EventArgs e) {
			if (!Validate())
				return;
			switch (await WebHandler.Register(user)) {
				case RegisterErrorState.Success:
					await WebHandler.Login(user);
					close();
					break;
				case RegisterErrorState.EmailFound:
					eEmail.BackgroundColor = Meh;
					break;
				case RegisterErrorState.NameFound:
					eUsername.BackgroundColor = Meh;
					break;
				case RegisterErrorState.Fail:
					break;
				default:
					break;
			}
		}
		string outputed = null;
		string encryptionPreview = null;
		private void Ep_TextChanged(object sender, TextChangedEventArgs e) {
			ep.BackgroundColor = ep.Text.Length < 5 ? Meh : Good;
			if (!string.IsNullOrEmpty(epa.Text)) {
				var eq = epa.Text == ep.Text;
				(sender as Entry).BackgroundColor = eq ? Good : Bad;
				if (eq)
					epa.BackgroundColor = ep.BackgroundColor = Good;
			}
			System.Threading.ThreadPool.QueueUserWorkItem((a) => encryptionPreview = System.Security.Cryptography.Encryption.EncryptPassword(eUsername.Text, a as string), (sender as Entry).Text);
		}
		private async void UpdatePreview() {
			while (true) {
				if (outputed != encryptionPreview) {
					outputed = encryptionPreview;
					lbEncrypt.Text = outputed;
				}
				await System.Threading.Tasks.Task.Delay(500);
			}
		}
	}
}