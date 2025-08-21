import { FFmpeg } from './ffmpeg/classes.js';
import { toBlobURL } from './util/index.js';

window.contentFFmpeg =
{
    Create: function ()
    {
        var ffmpeg = new FFmpeg();
        return nkJSObject.RegisterObject(ffmpeg);
    },

    Initialize: async function (uid)
    {
        try
        {
            var ffmpeg = nkJSObject.GetObject(uid);
            ffmpeg.on('log', ({ message }) =>
            {
                DotNet.invokeMethod('Gumknix', 'JsFFmpegOnLog', uid, message);
                console.log(message);
            });
            ffmpeg.onerror = (e) => console.error("error:", e);

            var jsUrl = await toBlobURL(`Content/FFmpeg/ffmpeg-core.js`, 'text/javascript');
            var wasmUrl = await toBlobURL(`Content/FFmpeg/ffmpeg-core.wasm`, 'application/wasm');
            //var workerUrl = await toBlobURL(`Content/FFmpeg/ffmpeg/worker.js`, 'text/javascript');
            await ffmpeg.load({
                coreURL: jsUrl,
                wasmURL: wasmUrl,
                //classWorkerURL: workerUrl
            });
            DotNet.invokeMethod('Gumknix', 'JsFFmpegOnInitialized', uid);
        }
        catch (e)
        {
            console.error('FFmpeg load failed:', e);
        }
    },

    Run: function (uid, d)
    {
        var ffmpeg = nkJSObject.GetObject(uid);

        var com = nkJSObject.ReadString(d);
        var args = com.split(' ');

        var fiid = Module.HEAP32[(d + 4) >> 2];
        var fi = nkJSObject.GetObject(fiid);

        var fr = new FileReader();
        fr.onloadend = async function ()
        {
            const arr = new Uint8Array(fr.result);
            await ffmpeg.writeFile(fi.name, arr);
            await ffmpeg.exec(args);
            const data = await ffmpeg.readFile('output.ogg');
            await ffmpeg.deleteFile(fi.name);
            var b = new Blob([data]);
            var bid = nkJSObject.RegisterObject(b);
            DotNet.invokeMethod('Gumknix', 'JsFFmpegOnComplete', uid, bid);
        };
        fr.readAsArrayBuffer(fi);
    }
};
