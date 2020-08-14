
using PunkteInWedding.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EditAccountPage : ContentPage {
		User user;
		User prevUser;
		string pw;
		void GenUser() => user = WebHandler.CreateUser(string.IsNullOrWhiteSpace(eUsername.Text.Trim()) || eUsername.Text.Trim() == user.Username ? null : eUsername.Text.Trim(), string.IsNullOrWhiteSpace(eEmail.Text.Trim()) || eEmail.Text.Trim() == user.Email ? null : eEmail.Text.Trim(), ep.Text);

		public EditAccountPage(string Password) {
			pw = Password;
			user = new User() { Password = Password, Username = WebHandler.shareUsername };
			InitializeComponent();
			Get();
		}
		public async void Get() {
			var g = await WebHandler.EditAcc.GetUserData(user.Password, user.Username);
			user.Username = g.Username;
			user.Email = g.Email;
			Show();
			prevUser = user;
		}
		void Show() {
			eEmail.Text = user.Email;
			eUsername.Text = user.Username;
		}
		static Color Bad => Color.FromRgba(1, 0, 0, 0.5);
		static Color Meh => Color.FromRgba(1, 1, 0, 0.5);
		static Color Good => Color.Transparent;
		bool Validate() {
			if (ep.Text != epa.Text) {
				epa.BackgroundColor = ep.BackgroundColor = Bad;
				return false;
			}
			else if (ep.Text != null && ep.Text.Length < 5) {
				ep.BackgroundColor = Meh;
				DisplayAlert("Warnung", "Passwörter müssen mindestens 5 Zeichen lang sein!", "aww");
				return false;
			}
			else
				epa.BackgroundColor = ep.BackgroundColor = Good;
			GenUser();
			if (user.Email != null)
				if (string.IsNullOrWhiteSpace(user.Email) || !WebHandler.IsValidEmail(user.Email)) {
					eEmail.BackgroundColor = Bad;
					return false;
				}
				else
					eEmail.BackgroundColor = Good;
			if (user.Username != null)
				if (user.Username.Contains("+") || user.Username.Contains("&")) {
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
		private async void Button_Clicked(object sender, System.EventArgs e) {
			if (!Validate())
				return;
			switch (await WebHandler.EditAcc.UpdateUser(prevUser.Username, user.Username, prevUser.Password, user.Password, user.Email)) {
				case System.RegisterErrorState.Success:
					await (Parent as NavigationPage).PopAsync();
					return;
				case System.RegisterErrorState.EmailFound:
					eEmail.BackgroundColor = Bad;
					break;
				case System.RegisterErrorState.NameFound:
					eUsername.BackgroundColor = Bad;
					break;
				default:
					break;
			}
		}

		private void Button_Clicked_1(object sender, System.EventArgs e) {
			(Parent as NavigationPage).PushAsync(new EditAccountDeletePage(pw));
		}
	}
}