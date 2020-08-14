
using PunkteInWedding.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ScoreboardPage : ContentPage {
		ScoreboardViewModel viewModel;

		public ScoreboardPage() {
			InitializeComponent();
			BindingContext = viewModel = new ScoreboardViewModel();
		}
		protected override void OnAppearing() {
			base.OnAppearing();
			if (viewModel.Users.Count == 0)
				viewModel.LoadItemsCommand.Execute(null);
		}

		private void LvUsers_ItemSelected(object sender, SelectedItemChangedEventArgs e) => lvUsers.SelectedItem = null;
	}
}