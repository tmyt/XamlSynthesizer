using Windows.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace XamlSynthesizer.Behaviors
{
    public class PlaySongAction : DependencyObject, IAction
    {
        public Song Song { get; set; }

        public object Execute(object sender, object parameter)
        {
            if (Song != null)
            {
                if (!Song.IsPlaying)
                {
                    Song.Play();
                }
            }
            return true;
        }
    }
}
