
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AboutPage : ContentPage {
		public AboutPage() {
			InitializeComponent();
			sid.Text = "";
			lbVersion.FormattedText = new FormattedString();
			lbVersion.FormattedText.Spans.Add(new Span() {
				Text = "Punkte In Wedding",
				FontAttributes = FontAttributes.Bold,
				TextColor = Color.FromHex("#FAFAFA"),
				FontSize = 30
			});
			lbVersion.FormattedText.Spans.Add(new Span() {
				Text = " ",
			});
			var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			lbVersion.FormattedText.Spans.Add(new Span() {
				Text = "" + v.Major + "." + v.Minor,
				FontAttributes = FontAttributes.Bold,
				ForegroundColor = (Color)this.Resources["LightTextColor"],
			});
			lbVersion.FormattedText.Spans.Add(new Span() {
				Text = "." + v.Build,
				FontAttributes = FontAttributes.Bold,
				ForegroundColor = (Color)this.Resources["LightTextColor"],
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label))
			});
		}

		private async void Button_Clicked(object sender, EventArgs e) {
			if (await DisplayAlert("Warnung", "Willst du dich wirklich ausloggen?", "Ja", "Nein"))
				await WebHandler.Logout();
		}

		private void Button_Clicked_1(object sender, EventArgs e) {
			if (sid.Text == "") {
				sid.Text = WebHandler.SID;
				(sender as Button).Text = "Hide SessionID";
			}
			else {
				sid.Text = "";
				(sender as Button).Text = "Show SessionID";
			}
		}

		private void Button_Clicked_2(object sender, EventArgs e) {
			Navigation.PushModalAsync(new EditAccountContainerPage());
		}
	}
}