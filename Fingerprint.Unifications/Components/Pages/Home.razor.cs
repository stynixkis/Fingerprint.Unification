using Microsoft.AspNetCore.Components;

namespace Fingerprint.Unifications.Components.Pages
{
	public class HomeModel : ComponentBase
	{
		[Inject]
		private NavigationManager NavigationManager { get; set; }
		public void NavigateToUpload()
		{
			NavigationManager.NavigateTo("/ChosingAction"); 
		}
	}
}