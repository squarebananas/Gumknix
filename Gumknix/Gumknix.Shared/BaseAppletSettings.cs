using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if BLAZORGL
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class BaseAppletSettings
    {
        public Dictionary<string, object> Settings { get; private set; }

        public bool Loading { get; private set; }
        public bool Saving { get; private set; }
        public bool SavePending { get; private set; }

#if BLAZORGL
        private FileSystemDirectoryHandle _AppletUserDataDirectory;
        private FileSystemFileHandle _AppletSettingsFile;
#endif

        public BaseAppletSettings() { }

        public void SetValue(string key, object value)
        {
            if (!Settings.TryAdd(key, value))
                Settings[key] = value;
            SavePending = true;
        }

        public void RemoveSetting(string key)
        {
            Settings.Remove(key);
            SavePending = true;
        }

        public void LoadSettings<T>(Gumknix gumknix, string appletName, string fileName = "Settings.json")
        {
#if BLAZORGL
            if ((gumknix.AppletUserDataStorage == null) || Loading)
                return;

            Loading = true;

            FileSystemDirectoryHandle appletUserDataStorageHandle = gumknix.AppletUserDataStorage.Handle as FileSystemDirectoryHandle;
            Task<FileSystemDirectoryHandle> directoryHandleTask = appletUserDataStorageHandle.GetDirectoryHandle(appletName, true);
            directoryHandleTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    _AppletUserDataDirectory = t.Result;
                    Task<FileSystemFileHandle> AppletSettingsFileTask = _AppletUserDataDirectory.GetFileHandle(fileName, true);
                    AppletSettingsFileTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            _AppletSettingsFile = t.Result;
                            Task<File> fileTask = _AppletSettingsFile.GetFile();
                            fileTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully && t.Result != null)
                                {
                                    File file = t.Result;
                                    Task<string> fileText = file.Text();
                                    fileText.ContinueWith(t =>
                                    {
                                        if (t.IsCompletedSuccessfully && t.Result != null && t.Result?.Length != 0)
                                        {
                                            try
                                            {
                                                Dictionary<string, object> loadedData =
                                                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(t.Result);
                                                if (loadedData != null)
                                                {
                                                    foreach (KeyValuePair<string, object> keyValuePair in loadedData)
                                                    {
                                                        System.Text.Json.JsonElement value = (System.Text.Json.JsonElement)keyValuePair.Value;

                                                        if (value.ValueKind == System.Text.Json.JsonValueKind.Array)
                                                        {
                                                            System.Text.Json.JsonElement.ArrayEnumerator elements = value.EnumerateArray();
                                                            System.Text.Json.JsonElement firstArrayElement = elements.FirstOrDefault();
                                                            switch (firstArrayElement.ValueKind)
                                                            {
                                                                case System.Text.Json.JsonValueKind.Number:
                                                                    loadedData[keyValuePair.Key] = elements.Select(e => e.GetInt32()).ToArray();
                                                                    break;
                                                                case System.Text.Json.JsonValueKind.String:
                                                                    loadedData[keyValuePair.Key] = elements.Select(e => e.GetString()).ToArray();
                                                                    break;
                                                            }
                                                        }
                                                    }

                                                    Settings = loadedData;
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        Settings ??= [];
                                        Loading = false;
                                    });
                                }
                            });
                        }
                    });
                }
            });
#endif
        }

        public void SaveSettings()
        {
            if (!SavePending || Saving)
                return;

            Saving = true;
            SavePending = false;

#if BLAZORGL
            if (_AppletSettingsFile == null)
                return;

            Task<FileSystemWritableFileStream> writableFileStreamTask = _AppletSettingsFile.CreateWritable();
            writableFileStreamTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    FileSystemWritableFileStream writableFileStream = t.Result;

                    string jsonSettings = System.Text.Json.JsonSerializer.Serialize(Settings);
                    byte[] data = Encoding.UTF8.GetBytes(jsonSettings);
                    Task<bool> writeTask = writableFileStream.Write(data);
                    writeTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            writableFileStream.Truncate((ulong)data.LongLength).ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                {
                                    Task closeTask = writableFileStream.Close();
                                    closeTask.ContinueWith(t =>
                                    {
                                        Saving = false;
                                    });
                                }
                            });
                        }
                    });
                }
            });
#endif
        }
    }
}
