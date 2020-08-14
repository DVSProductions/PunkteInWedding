using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EditAccountDeletePage : ContentPage {
		string pw;
		public EditAccountDeletePage(string password) {
			pw = password;
			InitializeComponent();
		}

		private async void Button_Clicked(object sender, EventArgs e) {
			try {
				await WebHandler.EditAcc.DeleteUser(pw);
			}
			catch {
				await DisplayAlert("Fehler", "Bei der Löschung ist etwas schiefgelaufen, bitte versuche es später erneut", "ok");
			}
		}
	}
}