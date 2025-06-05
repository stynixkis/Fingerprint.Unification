using Microsoft.AspNetCore.Components;

namespace Fingerprint.Unifications.Components.Pages
{
	public class ChoosingActionModel : ComponentBase
	{
		[Inject]
		private NavigationManager NavigationManager { get; set; }
		public void NavigateToDownload()
		{
			NavigationManager.NavigateTo("/UploadingAudioForComparison");
		}
	}
}
