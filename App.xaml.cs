namespace app;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) 
		{	MinimumWidth = 800,
        	MinimumHeight = 600,
			Title = "app" };
	}
}
