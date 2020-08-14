using Android.App;
using Android.Content.PM;
using Android.OS;
namespace PunkteInWedding.Droid {
	[Activity(Label = "Punkte in Wedding", Icon = "@mipmap/icon", MainLauncher = true, Theme = "@style/BootTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity {
		protected override void OnCreate(Bundle savedInstanceState) {
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;
			base.OnCreate(savedInstanceState);
			Xamarin.Forms.Forms.Init(this, savedInstanceState);
			LoadApplication(new App());
		}
	}
}