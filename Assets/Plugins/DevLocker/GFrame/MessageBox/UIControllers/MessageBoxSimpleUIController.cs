namespace DevLocker.GFrame.MessageBox.UIControllers
{
	/// <summary>
	/// Displays an in-game prompt with message for the user to answer.
	/// </summary>
	public class MessageBoxSimpleUIController : MessageBoxUIControllerBase
	{
		public MessageBoxUIText Title;
		public MessageBoxUIText Subtitle;
		public MessageBoxUIText TextContent;

		public override void Show(MessageData data)
		{
			Title.Text = data.Title;
			Subtitle.Text = data.Subtitle;
			TextContent.Text = data.Content;

			base.Show(data);
		}
	}
}