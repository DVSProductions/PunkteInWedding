
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Startpage : ContentPage {
		readonly Action OnDone;
		public Startpage(Action close) {
			OnDone = close;
			InitializeComponent();
			Chk();
		}
		public async void Chk() {
			if (!await WebHandler.TestInternet(this)) {
				System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
			}
		}
		private async void Login_Clicked(object sender, EventArgs e) => await Navigation.PushModalAsync(new LoginPage(OnDone));

		private async void Register_Clicked(object sender, EventArgs e) => await Navigation.PushModalAsync(new RegisterPage(OnDone));
	}
}