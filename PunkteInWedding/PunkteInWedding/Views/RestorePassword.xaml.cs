using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RestorePassword : ContentPage {
		public RestorePassword() => InitializeComponent();

		private async void ToolbarItem_Clicked(object sender, EventArgs e) => await Navigation.PopModalAsync();

		private async void Button_Clicked(object sender, EventArgs e) {
			await WebHandler.RestoreAccount(eEmail.Text);
			await Navigation.PopModalAsync();
		}
	}
}