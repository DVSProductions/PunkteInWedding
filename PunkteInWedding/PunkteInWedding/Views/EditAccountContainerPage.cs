
using Xamarin.Forms;

namespace PunkteInWedding.Views {
	public class EditAccountContainerPage : NavigationPage {
		public EditAccountContainerPage() {
			BackgroundColor = Color.FromRgb(30, 30, 30);
			var a = false;
			var b = new Page() { Title = "Account Bearbeiten", BackgroundColor = BackgroundColor };
			b.Appearing += (c, d) => { if (!a) Navigation.PopModalAsync(); };
			var e = new EditAccountPwPage(async (c) => { a = true; await PopAsync(); await PushAsync(c); a = false; });
			PushAsync(b);
			PushAsync(e);
		}
	}
}