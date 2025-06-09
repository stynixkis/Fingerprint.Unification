using Microsoft.AspNetCore.Components;

namespace Fingerprint.Unifications.Components.Pages
{
	public class UploadingAudioForComparisonModel : ComponentBase
	{
		[Inject]
		private NavigationManager NavigationManager { get; set; }
		public void NavigateToGenerate()
		{
			NavigationManager.NavigateTo("/WorkingWithFingerprint");
		}
	}
}
