using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください
using Media;
using ux;
using ux.Component;

namespace Sample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Media.AudioRenderer renderer;

        class Note
        {
            public long Duration { get; set; }
            public int Key { get; set; }
        }

        public MainPage()
        {
            this.InitializeComponent();

            renderer = new AudioRenderer();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //renderer.Stop();

            //renderer.Channels = 1;
            //renderer.BitsPerSample = 16;
            //renderer.SamplesPerSec = 44100;
            //renderer.Play();

            //Task.Run(new Action(Update));
            test.Play();
        }

        private async void Update()
        {
            var master = new Master(44100, 3);
            master.Play();
            master.PushHandle(new Handle(1, HandleType.Waveform, (int)WaveformType.Square));

            var bpm = 60;
            var tick = 60*1000/bpm/16;

            var score = new List<Note>();
            score.Add(new Note { Duration = 6, Key = ParseNote("C4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("D4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("E4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("F4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("G4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("A4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("B4") });
            score.Add(new Note { Duration = 6, Key = ParseNote("C5") });
            
            var samples = tick*44100/1000;
            var fb = new float[samples];
            var buffer = new byte[samples*2];
            var attack = false;

            while (score.Count != 0)
            {
                if (score[0].Duration-- == 0)
                {
                    master.PushHandle(new Handle(1, HandleType.NoteOff, score[0].Key, 0.5f));
                    score.RemoveAt(0);
                    attack = false;
                }
                else if(!attack)
                {
                    master.PushHandle(new Handle(1, HandleType.NoteOn, score[0].Key, 0.5f));
                    attack = true;
                }
                var k = master.Read(fb, 0, samples);
                for (int i = 0; i < samples * 2; )
                {
                    var sample = (ushort)(32767.0 * fb[i / 2] + 32768);
                    buffer[i++] = (byte)(sample & 0xff);
                    buffer[i++] = (byte)(sample >> 8);
                }
                renderer.AppendBuffer(buffer, true);
                await Task.Delay(tick);
            }
        }

        const string Notes = "CDEFGAB";
        static int[] NoteValues = { 0, 2, 4, 5, 7, 9, 11 };

        int ParseNote(string s)
        {

            if (s.Length != 2 && s.Length != 3) return 0;
            if (!Notes.Contains(s[0])) return 0;
            if (!Char.IsDigit(s[1])) return 0;
            if (s.Length == 3 && s[2] != '#' && s[2] != 'b') return 0;
            var value = 12*((s[1] - '0') + 2) + NoteValues[Notes.IndexOf(s[0])];
            if (s.Length == 3)
            {
                if (s[2] == '#') value += 1;
                if (s[2] == 'b') value -= 1;
            }
            return value;
        }

        private async Task<byte[]> BulidSamples(int hz, int sec)
        {
            var fb = new float[5 * 44100];
            var master = new Master(44100, 3);
            master.Play();
            master.PushHandle(new Handle(1, HandleType.Waveform, (int)WaveformType.Square));
            master.PushHandle(new Handle(1, HandleType.NoteOn, ParseNote("D4"), 0.5f));
            master.PushHandle(new Handle(2, HandleType.NoteOn, ParseNote("F4"), 0.5f));
            master.PushHandle(new Handle(3, HandleType.NoteOn, ParseNote("A4"), 0.5f));
            var k = master.Read(fb, 0, sec * 44100);

            var buffer = new byte[sec * 44100 * 2];
            var delta = Math.PI * 2 / (44100/hz);
            var n = 0.0;
            for (int i = 0; i < 44100*sec*2; )
            {
                /*
                var sample = (ushort)(32767.0 * Math.Sin(n) + 32768);
                buffer[i++] = (byte) (sample & 0xff);
                buffer[i++] = (byte)(sample >> 8);
                n += delta;
                 * */
                var sample = (ushort)(32767.0 * fb[i/2] + 32768);
                buffer[i++] = (byte)(sample & 0xff);
                buffer[i++] = (byte)(sample >> 8);
            }
            return buffer;
        }
    }
}
