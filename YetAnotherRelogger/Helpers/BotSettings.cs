using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using YetAnotherRelogger.Helpers.Bot;

namespace YetAnotherRelogger.Helpers
{

    #region BotSettings

    public sealed class BotSettings
    {
        #region singleton

        private static readonly BotSettings instance = new BotSettings();

        static BotSettings()
        {
        }

        private BotSettings()
        {
            Bots = new BindingList<BotClass>();
            settingsdirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Settings");
        }

        public static BotSettings Instance
        {
            get { return instance; }
        }

        #endregion

        private readonly string settingsdirectory;
        public BindingList<BotClass> Bots;

        public static string SettingsDirectory
        {
            get { return instance.settingsdirectory; }
        }

        public void Save()
        {
            var xml = new XmlSerializer(Bots.GetType());

            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);


            using (var writer = new StreamWriter(Path.Combine(SettingsDirectory, "Bots.xml")))
            {
                xml.Serialize(writer, Bots);
            }
        }

        public void Load()
        {
            var xml = new XmlSerializer(Bots.GetType());

            if (!File.Exists(Path.Combine(SettingsDirectory, "Bots.xml")))
                return;

            using (var reader = new StreamReader(Path.Combine(SettingsDirectory, "Bots.xml")))
            {
                Bots = xml.Deserialize(reader) as BindingList<BotClass>;
            }
        }

        public void Clone(int index)
        {
            var original = (BotClass) Bots[index].Clone();
            int nextIndex = index + 1;
            Bots.Insert(nextIndex, original);
        }
    }

    #endregion
}