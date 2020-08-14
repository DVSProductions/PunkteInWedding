using PunkteInWedding.Models;
using System;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoginPage : ContentPage {
		public User user { get; set; }
		Action close;
		public LoginPage(Action close) {
			this.close = close;
			InitializeComponent();
			user = new User();
			BindingContext = this;
			lbForgot.GestureRecognizers.Add(new TapGestureRecognizer() { Command = new Command(async (x) => await OpenRestore()) });
			UpdatePreview();
		}
		async System.Threading.Tasks.Task OpenRestore() => await Navigation.PushModalAsync(new NavigationPage(new RestorePassword()));
		async void Login_Clicked(object sender, EventArgs e) {
			if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 5)
				return;
			user.Username = user.Username.Trim();
			try {
				switch (await WebHandler.Login(user)) {
					case HttpStatusCode.Unauthorized:
						await DisplayAlert("Warnung", "Passwort oder Nutzername Falsch", "aww");
						break;
					case HttpStatusCode.OK:
						close();
						break;
					default:
						break;
				}
			}
			catch {
				await DisplayAlert("Fehler", "Verbindung zum Server fehlgeschlagen", "schade");
			}
		}
		string outputed = null;
		string encryptionPreview = null;
		private async void UpdatePreview() {
			while (true) {
				if (outputed != encryptionPreview)
					lbEncrypt.Text = outputed = encryptionPreview;
				await System.Threading.Tasks.Task.Delay(500);
			}
		}

		private void EPw_TextChanged(object sender, TextChangedEventArgs e) => System.Threading.ThreadPool.QueueUserWorkItem((a) => encryptionPreview = System.Security.Cryptography.Encryption.EncryptPassword(eName.Text, a as string), (sender as Entry).Text);
	}
}