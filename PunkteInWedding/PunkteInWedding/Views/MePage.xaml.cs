using PunkteInWedding.ViewModels;
using System;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MePage : ContentPage {
		public RecentViewModel viewModel;
		private Models.User MyUser;
		System.Diagnostics.Stopwatch s;
		public MePage() {
			InitializeComponent();
			Updater();
			(lvRecent.Parent as Frame).BackgroundColor = Color.FromRgba(1, 1, 1, 0.05);
			WebHandler.UpdateMe = new Action(async () => await Update());
			BindingContext = viewModel = new RecentViewModel();
			s = System.Diagnostics.Stopwatch.StartNew();
			System.Threading.ThreadPool.QueueUserWorkItem((e) => { System.Threading.Thread.Sleep(300); TimeBox.Text = s.Elapsed.TotalSeconds.ToString(); });
		}
		async Task<bool> Update() {
			if (!WebHandler.HasSIDFILE)
				return false;
			if (!await WebHandler.TestInternet(this)) return false;
			MyUser = await WebHandler.GetMe();
			lbMain.Text = WebHandler.shareUsername = MyUser.Username;
			lbScore.Text = MyUser.Score.ToString();
			return true;
		}
		async void Updater() {
			while (true) {
				if (!await Update())
					return;
				await Task.Delay(30_000);
			}
		}
		protected override void OnAppearing() {
			base.OnAppearing();
			if (viewModel.Activities.Count == 0)
				viewModel.UpdateActivities.Execute(null);
		}
		private async void Button_Clicked(object sender, EventArgs e) => await Navigation.PushModalAsync(new NavigationPage(new WaitForPage()));

		private void LvRecent_ItemSelected(object sender, SelectedItemChangedEventArgs e) => (sender as ListView).SelectedItem = null;
	}
}