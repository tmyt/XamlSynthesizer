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
            test.Play();
        }
    }
}
