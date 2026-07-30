namespace ExItS.PinoyBusinessPOS.Maui;

// Fully qualified: the sibling "ExItS.PinoyBusinessPOS.Application" project reference introduces a
// nested namespace named "Application" under the shared "ExItS.PinoyBusinessPOS" root, which shadows
// Microsoft.Maui.Controls.Application for a bare "Application" identifier in this namespace.
public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "ExItS.PinoyBusinessPOS.Maui" };
	}
}
