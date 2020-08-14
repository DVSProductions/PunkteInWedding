using PunkteInWedding.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PunkteInWedding.ViewModels {
	public class RecentViewModel : BaseViewModel {
		public ObservableCollection<TinyLog> Activities { get; protected set; }
		public Command UpdateActivities { get; protected set; }

		public RecentViewModel() {
			Activities = new ObservableCollection<TinyLog>();
			UpdateActivities = new Command(async () => await ExecuteLoadItemsCommand());
		}
		async Task ExecuteLoadItemsCommand() {
			if (IsBusy)
				return;
			if (!await WebHandler.HasInternet())
				return;
			IsBusy = true;
			try {
				var items = await WebHandler.GetRecentActions();
				if (items == null || items.Count == 0)
					return;
				Activities.Clear();
				var now = DateTime.Now;
				for (var n = items.Count - 1; n >= 0; n--) {
					var dt = new DateTime(long.Parse(items[n].Time));
					items[n].Time = dt.DayOfYear == now.DayOfYear && dt.Year == now.Year ? dt.ToShortTimeString() : dt.ToShortDateString();
					Activities.Add(items[n]);
				}
			}
			catch { }/*
			catch (Exception ex) {
				Debug.WriteLine(ex);
			}*/
			finally {
				IsBusy = false;
			}
		}
	}
}