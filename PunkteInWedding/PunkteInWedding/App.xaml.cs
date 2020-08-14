using PunkteInWedding.Views;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace PunkteInWedding {
	public partial class App : Application {
		/*
		//print all Special Folders and their contents
		void list(){
		foreach (var v in (Environment.SpecialFolder[])Enum.GetValues(typeof(Environment.SpecialFolder))) {
		var specialFolder = v;
		var path = Environment.GetFolderPath(specialFolder);
		Console.WriteLine("{0}={1}", specialFolder, path);
		if (path == null)
		continue;
		try {
		var directories = Directory.EnumerateDirectories(path);
		foreach (var directory in directories) {
		Console.WriteLine(directory);
		}
		}
		catch { }
		}
		}
		*/
		public App() => InitializeComponent();//list();
		/// <summary>
		/// Check if we are logged in. if we are, show the main page. else show the login page
		/// </summary>
		void Switcheroo() => MainPage = WebHandler.HasSIDFILE ? (Page)new MainPage() : new Startpage(() => MainPage = new MainPage());
		/// <summary>
		/// Creates a Popup message 
		/// </summary>
		/// <param name="a">Message text</param>
		/// <returns></returns>
		Task ShowWarning(string a) => MainPage.DisplayAlert("Warnung", a, "Nochmal");
		/// <summary>
		/// Array containing all test results
		/// </summary>
		bool?[] t = new bool?[5];
		/// <summary>
		/// Run all internal tests
		/// </summary>
		void RunTests() {
			t = new bool?[5];
			ThreadPool.QueueUserWorkItem((a) => t[0] = WebHandler.PingSync("google.com", 80));
			ThreadPool.QueueUserWorkItem((a) => t[1] = WebHandler.PingSync(WebHandler.Domain, 80));
			ThreadPool.QueueUserWorkItem((a) => t[2] = WebHandler.PingSync("portquiz.net", WebHandler.port));
			ThreadPool.QueueUserWorkItem(async (a) => t[3] = await WebHandler.PingDVS().ConfigureAwait(true));
			ThreadPool.QueueUserWorkItem((a) => t[4] = WebHandler.CheckJsonWorking());
		}
		protected override async void OnStart() {
			MainPage = new Page();
			RunTests();
			ThreadPool.QueueUserWorkItem((a) => { var b = WebHandler.HasSIDFILE; });
			var counter = 0;
			do {
				if (t[0] == false) {
					counter++;
					if (counter < 3) {
						RunTests();
						continue;
					}
					counter = 0;
					await ShowWarning("Konnte keine Verbindung mit dem Internet herstellen");
					RunTests();
				}
				else if (t[1] == false) {
					counter++;
					if (counter < 3) continue;
					counter = 0;
					await ShowWarning("Konnte keine Verbindung mit dem Server herstellen");
					RunTests();
				}
				else if (t[2] == false && t[3] == false) {
					counter++;
					if (counter < 3) continue;
					counter = 0;
					await ShowWarning("Du bist in einem Netzwerk in dem der Port 50001 blockiert wird");
					RunTests();
				}
				else if (t[3] == false) {
					counter++;
					if (counter < 3) continue;
					counter = 0;
					await ShowWarning("Der Server ist derzeit down. Bitte versuche es später erneut");
					RunTests();
				}
				else if (t[4] == false) {
					counter++;
					if (counter < 3) continue;
					await MainPage.DisplayAlert("Kritischer Fehler!", "Das JSON Modul ist beschädigt!", "verdammt");
					Current.Quit();
					Quit();
					System.Environment.Exit(666);
					return;
				}
			} while (t[3] != true || t[4] != true);
			WebHandler.DaddyLogoutDetection = Switcheroo;
			WebHandler.DaddyLogoutDetection();
		}

		protected override void OnSleep() {
			// Handle when your app sleeps
		}

		protected override void OnResume() {
			// Handle when your app resumes
		}
	}
}
