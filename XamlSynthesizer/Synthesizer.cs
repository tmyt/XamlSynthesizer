using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Media;
using ux;
using ux.Component;
using XamlSynthesizer.Base;
using XamlSynthesizer.Internal;
using Note = XamlSynthesizer.Base.Note;

namespace XamlSynthesizer
{
    [ContentProperty(Name = "Tracks")]
    public class Song : DependencyObject
    {
        public Song()
        {
            Tracks = new TrackCollection();
        }

        public TrackCollection Tracks
        {
            get { return (TrackCollection)GetValue(TracksProperty); }
            set { SetValue(TracksProperty, value); }
        }
        public int SamplingRate
        {
            get { return (int)GetValue(SamplingRateProperty); }
            set { SetValue(SamplingRateProperty, value); }
        }

        public int Tempo
        {
            get { return (int)GetValue(TempoProperty); }
            set { SetValue(TempoProperty, value); }
        }

        public static readonly DependencyProperty TracksProperty =
            DependencyProperty.Register("Tracks", typeof(TrackCollection), typeof(Song), new PropertyMetadata(null));
        public static readonly DependencyProperty SamplingRateProperty =
            DependencyProperty.Register("SamplingRate", typeof(int), typeof(Song), new PropertyMetadata(44100));
        public static readonly DependencyProperty TempoProperty =
            DependencyProperty.Register("Tempo", typeof(int), typeof(Song), new PropertyMetadata(120));

        public bool IsPlaying { get; private set; }

        public void Play()
        {
            if(IsPlaying) return;
            var sequencer = new Sequencer(Tracks, SamplingRate, Tempo);
            sequencer.Run(this);
            IsPlaying = true;
        }

        private class Sequencer
        {
            private TrackCollection tracks;
            private int samplesPerSec;
            private int tempo;

            private int tick;
            private int samples;
            private float[] fb;
            private byte[] buffer;
            private int activeTracks;

            private List<List<Internal.Note>> score;

            private AudioRenderer renderer;
            private Master master;

            private DispatcherTimer timer;

            public Sequencer(TrackCollection tracks, int samplesPerSec, int bpm)
            {
                this.tracks = tracks;
                this.samplesPerSec = samplesPerSec;
                this.tempo = bpm;

                // オーディオレンダラを初期化
                renderer = new AudioRenderer();
                renderer.Channels = 2;
                renderer.BitsPerSample = 16;
                renderer.SamplesPerSec = samplesPerSec;
                renderer.Play();

                // メディアレンダラを初期化
                master = new Master(samplesPerSec, tracks.Count * 16);
                master.Play();

                // 音色を設定するべき
                // master.PushHandle(new Handle(1, HandleType.Waveform, (int)WaveformType.Square));

                // 内部表現に変換
                score = new List<List<Internal.Note>>();
                foreach (var track in tracks)
                {
                    var notes = new List<Internal.Note>();
                    notes.AddRange(track.Notes.Select(n =>
                    {
                        var note = new Internal.Note
                        {
                            Duration = (int)n.Length,
                            IsTone = n is Tone
                        };
                        if (n.Flags.HasFlag(NoteFlags.Dotted))
                        {
                            note.Duration += note.Duration / 2;
                        }
                        if (n.Flags.HasFlag(NoteFlags.Tie))
                        {
                            note.IsTie = true;
                        }
                        if (n.Flags.HasFlag(NoteFlags.Triplet))
                        {
                            note.Duration /= 3;
                        }
                        if (note.IsTone)
                        {
                            note.Keys = (n as Tone).Scale.Split(',').Select(ParseNote).ToArray();
                        }
                        note.OriginalDuration = note.Duration;
                        return note;
                    }));
                    notes.Add(new Internal.Note { Duration = -1 }); // Tail mark
                    score.Add(notes);
                }

                // tickを計算
                tick = 60 * 1000 / tempo / 16;
                samples = tick * 44100 / 1000 * 2;
                activeTracks = tracks.Count;
            }

            public void Run(Song song)
            {
                //timer = new DispatcherTimer();
                //timer.Interval = TimeSpan.FromMilliseconds(tick);
                //timer.Tick += Update;
                //timer.Start();
                Task.Run(() => Update(song, null));
            }

            private async void Update(Song song, object b)
            {
                //if (activeTracks == 0)
                //{
                //    timer.Stop();
                //    return;
                //}

                fb = new float[samples * 10];
                buffer = new byte[samples * 2 * 10];

                for (int i = 0; i < score.Count * 16; ++i)
                {
                    // 音色を設定してみる
                    master.PushHandle(new Handle(i, HandleType.Waveform, (int)WaveformType.FM));
                    master.PushHandle(new Handle(i, HandleType.EditWaveform, (int)(FMOperate.Op0 | FMOperate.Send0), 0.8f));
                    master.PushHandle(new Handle(i, HandleType.EditWaveform, (int)(FMOperate.Op1 | FMOperate.Send0), 0.5f));
                    master.PushHandle(new Handle(i, HandleType.EditWaveform, (int)(FMOperate.Op1 | FMOperate.Freq), 2.0f));
                    master.PushHandle(new Handle(i, HandleType.Envelope, (int)EnvelopeOperate.Attack, 0.0f));
                    master.PushHandle(new Handle(i, HandleType.Envelope, (int)EnvelopeOperate.Decay, 3.0f));
                    master.PushHandle(new Handle(i, HandleType.Envelope, (int)EnvelopeOperate.Sustain, 0.0f));
                }

                while (activeTracks != 0)
                {
                    for (int o = 0; o < 10; ++o)
                    {
                        for (int tno = 0; tno < score.Count; tno++)
                        {
                            var track = score[tno];
                            // Track completed
                            if (track[0].Duration == -1)
                            {
                                activeTracks--;
                                score.RemoveAt(0);
                                continue;
                            }
                            // Note off
                            if (track[0].Duration-- == 0)
                            {
                                //if (track[0].IsTone && track[0].IsTie == false && track[0].IsAttack)
                                //{
                                //    for (int i = 0; i < track[0].Keys.Length; ++i)
                                //    {
                                //        master.PushHandle(new Handle(16 * tno + i + 1, HandleType.NoteOff,
                                //            track[0].Keys[i], 0.5f));
                                //    }
                                //}
                                track.RemoveAt(0);
                            }
                            else if (track[0].OriginalDuration * 0.2 > track[0].Duration)
                            //else if (track[0].Duration == 0)
                            {
                                if (track[0].IsTone && track[0].IsTie == false && track[0].IsAttack)
                                {
                                    for (int i = 0; i < track[0].Keys.Length; ++i)
                                    {
                                        master.PushHandle(new Handle(16 * tno + i + 1, HandleType.NoteOff,
                                            track[0].Keys[i], 0.5f));
                                    }
                                }
                            }
                            // Note on
                            else if (!track[0].IsAttack)
                            {
                                if (track[0].IsTone)
                                {
                                    for (int i = 0; i < track[0].Keys.Length; ++i)
                                    {
                                        master.PushHandle(new Handle(16 * tno + i + 1, HandleType.NoteOn, track[0].Keys[i],
                                            0.5f));
                                    }
                                }
                                track[0].IsAttack = true;
                            }
                            else
                            {
                                if (track[0].IsTone)
                                {
                                    for (int i = 0; i < track[0].Keys.Length; ++i)
                                    {
                                        master.PushHandle(new Handle(16 * tno + i + 1, HandleType.Envelope,
                                            (float)track[0].Duration / track[0].OriginalDuration));
                                    }
                                }
                            }
                        }
                        master.Read(fb, o * samples, samples);
                    }
                    for (int i = 0; i < samples * 2 * 10; )
                    {
                        var sample = (ushort)(32767.0 * fb[i / 2] + 32768);
                        buffer[i++] = (byte)(sample & 0xff);
                        buffer[i++] = (byte)(sample >> 8);
                    }
                    renderer.AppendBuffer(buffer, false);
                    await Task.Delay(tick * 10);
                }
                song.IsPlaying = false;
            }

            static string Notes = "CDEFGAB";
            static int[] NoteValues = { 0, 2, 4, 5, 7, 9, 11 };

            int ParseNote(string s)
            {

                if (s.Length != 2 && s.Length != 3) return 0;
                if (!Notes.Contains(s[0])) return 0;
                if (!Char.IsDigit(s[1])) return 0;
                if (s.Length == 3 && s[2] != '#' && s[2] != 'b') return 0;
                var value = 12 * ((s[1] - '0') + 2) + NoteValues[Notes.IndexOf(s[0])];
                if (s.Length == 3)
                {
                    if (s[2] == '#') value += 1;
                    if (s[2] == 'b') value -= 1;
                }
                return value;
            }
        }
    }

    [ContentProperty(Name = "Notes")]
    public class Track : DependencyObject
    {
        public Track()
        {
            Notes = new NoteCollecton();
        }

        public NoteCollecton Notes
        {
            get { return (NoteCollecton)GetValue(NotesProperty); }
            set { SetValue(NotesProperty, value); }
        }
        public Voice Voice
        {
            get { return (Voice)GetValue(VoiceProperty); }
            set { SetValue(VoiceProperty, value); }
        }

        public static readonly DependencyProperty NotesProperty =
            DependencyProperty.Register("Notes", typeof(NoteCollecton), typeof(Track), new PropertyMetadata(null));
        public static readonly DependencyProperty VoiceProperty =
            DependencyProperty.Register("Voice", typeof(Voice), typeof(Track), new PropertyMetadata(null));
    }

    public class Tone : Note
    {
        public string Scale
        {
            get { return (string)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(string), typeof(Tone), new PropertyMetadata(null));
    }

    public class Rest : Note
    {

    }

    [ContentProperty(Name = "Parameters")]
    public class Voice : DependencyObject
    {
        public Voice()
        {
            Parameters = new ParameterCollection();
        }

        public ParameterCollection Parameters
        {
            get { return (ParameterCollection)GetValue(ParametersProperty); }
            set { SetValue(ParametersProperty, value); }
        }

        public static readonly DependencyProperty ParametersProperty =
            DependencyProperty.Register("Parameters", typeof(ParameterCollection), typeof(Voice), new PropertyMetadata(null));
    }

    public class Parameter : DependencyObject
    {
        public Operator Source
        {
            get { return (Operator)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        public Operator Target
        {
            get { return (Operator)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Operator), typeof(Parameter), new PropertyMetadata(Operator.Invalid));
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(Operator), typeof(Parameter), new PropertyMetadata(Operator.Invalid));
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(Parameter), new PropertyMetadata(0.0f));
    }

    public enum NoteLength
    {
        WholeNote = 64,
        HalfNote = 32,
        QuarterNote = 16,
        EighthNote = 8,
        SixtheenthNote = 4,
        ThirtySecondNote = 2,
        SixtyFourthNote = 1
    }

    [Flags]
    public enum NoteFlags
    {
        Dotted,
        Triplet,
        Tie
    }

    public enum Operator
    {
        Invalid,
        Op1,
        Op2,
        Op3,
        Op4,
        Out,
        Frequency
    }
}
