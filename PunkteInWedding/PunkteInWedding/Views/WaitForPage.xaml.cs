using PunkteInWedding.Models;
using PunkteInWedding.ViewModels;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PunkteInWedding.Views {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WaitForPage : ContentPage {
		FilterableViewModel viewModel;
		public WaitForPage() {
			InitializeComponent();
			BindingContext = viewModel = new FilterableViewModel();
			sb.SearchCommand = new Command(() => { viewModel.Filter = sb.Text.ToLower(); viewModel.DoFilter.Execute(null); });
		}

		private async void ToolbarItem_Clicked(object sender, EventArgs e) => await Navigation.PopModalAsync();

		protected override void OnAppearing() {
			base.OnAppearing();
			if (viewModel.Users.Count == 0)
				viewModel.LoadItemsCommand.Execute(null);
		}

		private void Sb_TextChanged(object sender, TextChangedEventArgs e) => sb.SearchCommand.Execute(null);

		private async void ListView_ItemTapped(object sender, ItemTappedEventArgs e) {
			var elem = e.Item as User;
			if (await DisplayAlert("Bestätigen", "Wartest du gerade auf " + elem.Username + "?", "Ja", "Nein")) {
				await Navigation.PopModalAsync();
				await WebHandler.WaitOn(elem.Id);
				WebHandler.UpdateMe();
			}
		}

		private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e) {
			(sender as ListView).SelectedItem = null;
		}
	}
}