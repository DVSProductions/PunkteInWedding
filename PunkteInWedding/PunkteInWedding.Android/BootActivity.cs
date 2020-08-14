using Android.App;
using Android.Content;
namespace PunkteInWedding.Droid.Resources {
	[Activity(Theme = "@style/BootTheme", MainLauncher = false, NoHistory = true)]
	public class BootActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {
		// Launches the startup task
		protected override void OnResume() {
			base.OnResume();
			//StartActivity(new Intent(Application.Context, typeof(MainActivity)));
		}
	}
}