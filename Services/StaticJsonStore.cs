using System.Text.Json;

namespace MLYSO.Web.Services
{
    public class StaticJsonStore<T> where T : class, new()
    {
        private readonly string _file;
        private static readonly JsonSerializerOptions _opt = new() { WriteIndented = true };

        //  Process-wide lock: tüm instance’lar ortak kilit
        private static readonly object _globalLock = new();

        public StaticJsonStore(IWebHostEnvironment env, string name)
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data", "static");
            Directory.CreateDirectory(dir);
            _file = Path.Combine(dir, name.EndsWith(".json") ? name : $"{name}.json");

            // İlk oluşturma
            if (!File.Exists(_file))
            {
                var tmp = _file + ".tmp";
                var json = JsonSerializer.Serialize(new T(), _opt);
                File.WriteAllText(tmp, json);
                File.Move(tmp, _file); 
            }
        }

        public T Read()
        {
            lock (_globalLock)
            {
                // Başka bir işlem yazıyor olabilir: paylaşımlı read ile dene
                using var fs = new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var json = sr.ReadToEnd();
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }
        }

        public void Write(T model)
        {
            lock (_globalLock)
            {
                // Atomic write: 
                var tmp = _file + ".tmp";
                var json = JsonSerializer.Serialize(model, _opt);

                // Retry (virüs)
                const int maxTry = 5;
                var delay = 40; // ms
                for (int i = 0; i < maxTry; i++)
                {
                    try
                    {
                        File.WriteAllText(tmp, json);
                        if (File.Exists(_file))
                            File.Replace(tmp, _file, null);
                        else
                            File.Move(tmp, _file);
                        return;
                    }
                    catch (IOException) when (i < maxTry - 1)
                    {
                        Thread.Sleep(delay);
                        delay *= 2;
                    }
                }

                // son deneme (hata)
                File.WriteAllText(tmp, json);
                if (File.Exists(_file))
                    File.Replace(tmp, _file, null);
                else
                    File.Move(tmp, _file);
            }
        }
    }
}
