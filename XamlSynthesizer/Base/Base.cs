using System.Collections.Generic;
using Windows.UI.Xaml;

namespace XamlSynthesizer.Base
{
    public abstract class Note : DependencyObject
    {
        public NoteLength Length
        {
            get { return (NoteLength)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }
        public NoteFlags Flags
        {
            get { return (NoteFlags)GetValue(FlagsProperty); }
            set { SetValue(FlagsProperty, value); }
        }

        public static readonly DependencyProperty LengthProperty =
            DependencyProperty.Register("Length", typeof(NoteLength), typeof(Note), new PropertyMetadata(NoteLength.WholeNote));
        public static readonly DependencyProperty FlagsProperty =
            DependencyProperty.Register("Flags", typeof(NoteFlags), typeof(Note), new PropertyMetadata(0));
    }

    public class TrackCollection : List<Track>
    {

    }

    public class NoteCollecton : List<Note>
    {

    }

    public class ParameterCollection : List<Parameter>
    {
        
    }
}
