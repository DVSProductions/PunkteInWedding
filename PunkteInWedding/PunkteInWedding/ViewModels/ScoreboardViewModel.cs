using PunkteInWedding.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PunkteInWedding.ViewModels {
	public class ScoreboardViewModel : BaseViewModel {
		public ObservableCollection<User> Users { get; protected set; }
		public Command LoadItemsCommand { get; protected set; }

		public ScoreboardViewModel() {
			Users = new ObservableCollection<User>();
			LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
		}
		async Task ExecuteLoadItemsCommand() {
			if (IsBusy)
				return;
			IsBusy = true;
			try {
				var items = await WebHandler.GetScoreboard();
				if (items == null || items.Count == 0)
					return;
				Users.Clear();
				int idx = 1;
				foreach (var item in items) {
					item.Password = idx++.ToString();
					Users.Add(item);
				}
			}
			catch { }
			/*
			catch (Exception ex) {
				Debug.WriteLine(ex);
			}*/
			finally {
				IsBusy = false;
			}
		}
	}
}