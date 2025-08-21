using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using nkast.Wasm.JSInterop;
using nkast.Wasm.File;

namespace Gumknix
{
    public class ModuleFFmpeg : CachedJSObject<ModuleFFmpeg>
    {
        public delegate void OnLogDelegate(object sender, string message);
        public delegate void OnCompleteDelegate(object sender, int byteArrayUid);

        public event EventHandler<EventArgs> OnInitialized;
        public event OnLogDelegate OnLog;
        public event OnCompleteDelegate OnComplete;

        internal ModuleFFmpeg(int uid) : base(uid)
        {
        }

        public static ModuleFFmpeg Create()
        {
            int uid = JSObject.StaticInvokeRetInt("contentFFmpeg.Create");
            if (uid == -1)
                return null;

            ModuleFFmpeg ffmpeg = ModuleFFmpeg.FromUid(uid);
            if (ffmpeg != null)
                return ffmpeg;

            return new ModuleFFmpeg(uid);
        }

        public void Initialize()
        {
            Invoke("contentFFmpeg.Initialize");
        }

        public void Run(string command, File inputFile)
        {
            Invoke("contentFFmpeg.Run", command, inputFile.Uid);
        }

        [JSInvokable]
        public static void JsFFmpegOnInitialized(int uid)
        {
            ModuleFFmpeg ffmpeg = ModuleFFmpeg.FromUid(uid);
            if (ffmpeg == null)
                return;

            var handler = ffmpeg.OnInitialized;
            if (handler != null)
                handler(ffmpeg, EventArgs.Empty);
        }

        [JSInvokable]
        public static void JsFFmpegOnLog(int uid, string message)
        {
            ModuleFFmpeg ffmpeg = ModuleFFmpeg.FromUid(uid);
            if (ffmpeg == null)
                return;

            OnLogDelegate handler = ffmpeg.OnLog;
            if (handler != null)
                handler(ffmpeg, message);
        }

        [JSInvokable]
        public static void JsFFmpegOnComplete(int uid, int byteArrayUid)
        {
            ModuleFFmpeg ffmpeg = ModuleFFmpeg.FromUid(uid);
            if (ffmpeg == null)
                return;

            OnCompleteDelegate handler = ffmpeg.OnComplete;
            if (handler != null)
                handler(ffmpeg, byteArrayUid);
        }
    }
}
