using PunkteInWedding.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PunkteInWedding.ViewModels {
	class FilterableViewModel : ScoreboardViewModel {
		List<User> unfiltered;
		public string Filter { get; set; }
		public Command DoFilter { get; }
		public FilterableViewModel() {
			Filter = "";
			unfiltered = new List<User>();
			Users = new ObservableCollection<User>();
			LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
			DoFilter = new Command(() => doFilter());
		}
		void doFilter() {
			Users.Clear();
			string unLower = WebHandler.shareUsername.ToLower();
			foreach (var u in unfiltered) {
				var lo = u.Username.ToLower();
				if ((string.IsNullOrWhiteSpace(Filter) || lo.StartsWith(Filter)) && lo != unLower)
					Users.Add(u);
			}
		}
		async Task ExecuteLoadItemsCommand() {
			if (IsBusy)
				return;
			IsBusy = true;
			try {
				var newUsers = await WebHandler.GetScoreboard();
				if (newUsers == null || newUsers.Count == 0)
					return;
				unfiltered = newUsers;
				doFilter();
			}
			catch {
			}
			finally {
				IsBusy = false;
			}
		}
	}
}
