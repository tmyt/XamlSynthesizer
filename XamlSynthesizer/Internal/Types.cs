using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace XamlSynthesizer.Internal
{
    class Note
    {
        public long Duration { get; set; }
        public long OriginalDuration { get; set; }
        public int[] Keys { get; set; }
        public bool IsTone { get; set; }
        public bool IsAttack { get; set; }
        public bool IsTie { get; set; }
    }

    static class DispatcherHelper
    {
        public static async Task<T> Read<T>(Func<T> getter)
        {
            T result = default(T);
            var disp = CoreWindow.GetForCurrentThread().Dispatcher;
            await disp.RunAsync(CoreDispatcherPriority.High, () => { result = getter(); });
            return result;
        }
    }
}
