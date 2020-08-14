using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EditAccountPwPage : ContentPage {
		Action<EditAccountPage> back;
		public EditAccountPwPage(Action<EditAccountPage> killer) {
			back = killer;
			InitializeComponent();
		}
		private async void Button_Clicked(object sender, EventArgs e) {
			try {
				if (await WebHandler.EditAcc.PWCheck(ePassword.Text))
					back(new EditAccountPage(ePassword.Text));
				else
					ePassword.BackgroundColor = Color.FromRgba(1, 0, 0, 0.5);
			}
			catch {
				await DisplayAlert("Fehler", "Netzwerkfehler bei der Anfrage", "aww");
			}
		}
	}
}