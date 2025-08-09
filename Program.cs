using ImGuiNET;
using ClickableTransparentOverlay;
using System.Numerics;
using System.Drawing;
using Tesseract;
using System.Drawing.Imaging;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Reflection;
using System.Media;

namespace DungeonNotifier
{
    public class Program : Overlay
    {
        private bool IsFoundDungeon { get; set; }
        private Vector2 screenSize = new Vector2(3440, 1440);
        private Vector2 notificationPosition = new Vector2(2980, 15);
        private Vector2 notificationSize = new Vector2(30, 30);
        private Vector4 notificationColor = new Vector4(0, 255, 55, 255);
        private Vector4 debugOverlayColor = new Vector4(255, 255, 255, 255);
        private Rectangle imageCheckPosition = new Rectangle(0, 1000, 600, 440);
        private int checkDelay = 1;

        private ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        private object dataLocker = new object();
        private string[] targetDungeons = new string[]
        {
            "woodland",
            "snake",
            "magic",
            "sprite",
            "nest"
        };
        private List<string> foundDungeons = new List<string>();
        private List<string> newFoundDungeons = new List<string>();
        private CancellationTokenSource source;
        private StringBuilder currentText = new StringBuilder();
        private string m_DungeonInputString = string.Empty;
        private int m_DungeonTmp;
        private bool m_DebugOverlayPosSize;
        private SoundPlayer m_NotifPlayer = new SoundPlayer();
        private List<string> m_NotifSounds = new List<string>();
        private int m_SelectedNotifSoundIndex;



        public Program()
        {
            source = new CancellationTokenSource();

            DetectNotifSounds();
            LoadSettings();

            StartImageChecking(source.Token);
            Start().Wait();
        }

        public static void Main(string[] args)
        {
            new Program();
        }

        protected override void Render()
        {
            DrawMenu();
            DrawOverlay();
        }

        private void DrawMenu()
        {
            ImGui.Begin("Realm Dungeon Notifier");

            if (ImGui.Button("Close"))
            {
                source.Cancel();
                Close();
            }

            if (ImGui.CollapsingHeader("Game Settings"))
            {
                var checkRect = new Vector4(imageCheckPosition.X, imageCheckPosition.Y, imageCheckPosition.Width, imageCheckPosition.Height);

                ImGui.DragFloat4("Image Check Position", ref checkRect);

                imageCheckPosition.X = (int)checkRect.X;
                imageCheckPosition.Y = (int)checkRect.Y;
                imageCheckPosition.Width = (int)checkRect.Z;
                imageCheckPosition.Height = (int)checkRect.W;

                ImGui.DragFloat2("Notification Position", ref notificationPosition);

                if (ImGui.CollapsingHeader("Debug Overlay Color"))
                {
                    ImGui.ColorPicker4("Debug Overlay Color", ref debugOverlayColor);
                }

                ImGui.Checkbox("Debug Overlay", ref m_DebugOverlayPosSize);
            }

            if (ImGui.CollapsingHeader("Dungeon Settings"))
            {
                ImGui.InputText("Dungeon to add", ref m_DungeonInputString, 100u);

                //ImGui.SameLine();

                var newDungeonsList = new List<string>(targetDungeons);

                if (ImGui.Button("Add"))
                {
                    if (!string.IsNullOrEmpty(m_DungeonInputString))
                    {
                        newDungeonsList.Add(m_DungeonInputString);

                        m_DungeonInputString = string.Empty;
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Remove"))
                {
                    if (newDungeonsList.Count > 0)
                    {
                        newDungeonsList.RemoveAt(m_DungeonTmp);
                    }
                }

                ImGui.ListBox("Dungeons:", ref m_DungeonTmp, targetDungeons, targetDungeons.Length);

                if (newDungeonsList.Count != targetDungeons.Length)
                {
                    targetDungeons = newDungeonsList.ToArray();
                }
            }

            if (ImGui.Button("Save Settings"))
            {
                SaveSettings();
            }

            if (ImGui.Button("Detect Notif Sound"))
            {
                DetectNotifSounds();
            }

            if (m_NotifSounds.Count > 0)
            {
                ImGui.Combo("Selected Notif Sound", ref m_SelectedNotifSoundIndex, m_NotifSounds.ToArray(), m_NotifSounds.Count);
            }

            if (ImGui.Button("Test Notif Sound"))
            {
                if (m_NotifSounds.Count > 0 && File.Exists(m_NotifSounds[m_SelectedNotifSoundIndex]))
                {
                    Beep();
                }
            }

            ImGui.Text(currentText.ToString());

            ImGui.End();
        }

        private void DrawOverlay()
        {
            if (m_DebugOverlayPosSize)
            {
                ImGui.Begin("Overlay", flags);

                ImDrawListPtr overlayPtr = ImGui.GetWindowDrawList();

                ImGui.SetWindowPos(new Vector2(imageCheckPosition.X, imageCheckPosition.Y));

                ImGui.TextUnformatted("This is where the app will check for incoming dungeons");

                ImGui.SetWindowPos(new Vector2(notificationPosition.X, notificationPosition.Y));

                ImGui.TextUnformatted("Notification Position");

                ImGui.SetWindowPos(new Vector2(0, 0));

                var renderOriginX = imageCheckPosition.X;
                var renderOriginY = imageCheckPosition.Y;

                var renderWidth = imageCheckPosition.Width;
                var renderHeight = imageCheckPosition.Height;

                var overlayMin = new Vector2(renderOriginX, renderOriginY);
                var overlayMax = new Vector2(renderOriginX + renderWidth, renderOriginY + renderHeight);

                overlayPtr.AddRect(overlayMin, overlayMax, ImGui.ColorConvertFloat4ToU32(debugOverlayColor));

                var notifMin = new Vector2(notificationPosition.X, notificationPosition.Y);
                var notifMax = new Vector2(notificationPosition.X + notificationSize.X, notificationPosition.Y + notificationSize.Y);

                overlayPtr.AddRect(notifMin, notifMax, ImGui.ColorConvertFloat4ToU32(debugOverlayColor));

                ImGui.End();
            }

            //overlayPtr.AddRect(windowMin, windowMax, ImGui.ColorConvertFloat4ToU32(gameOverlayColor));

            bool foundSomething = false;
            List<string> dungeons = null;

            lock (dataLocker)
            {
                if (IsFoundDungeon)
                {
                    foundSomething = IsFoundDungeon;

                    if (newFoundDungeons.Count > foundDungeons.Count)
                    {
                        Beep();
                    }


                    dungeons = newFoundDungeons;
                }
            }

            foundDungeons = newFoundDungeons;

            if (foundSomething)
            {
                ImGui.SetNextWindowSize(screenSize);
                ImGui.SetNextWindowPos(new Vector2(0, 0));

                ImGui.Begin("Overlay", flags);

                ImDrawListPtr drawlist = ImGui.GetWindowDrawList();

                drawlist.AddRectFilled(notificationPosition, notificationPosition + notificationSize, ImGui.ColorConvertFloat4ToU32(notificationColor));

                ImGui.End();

                if (dungeons == null)
                    return;

                ImGui.SetNextWindowPos(notificationPosition + new Vector2(0, notificationSize.Y));
                ImGui.Begin("Overlay", flags);
                ImGui.Text("Found:");

                foreach (var dungeon in dungeons)
                {
                    ImGui.Text(dungeon);
                }

                ImGui.End();
            }
        }

        public Task StartImageChecking(CancellationToken token)
        {
            Task t = new Task(DoImageChecking, token);

            t.Start();

            return t;
        }

        private void DoImageChecking()
        {
            Bitmap bmp = new Bitmap(imageCheckPosition.Width, imageCheckPosition.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            while (true)
            {
                g.CopyFromScreen(imageCheckPosition.Left, imageCheckPosition.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

                using (var tesEngine = new TesseractEngine("F:\\_personal\\c_sharp\\DungeonNotifier\\tessdata", "eng", EngineMode.Default))
                {
                    var textData = tesEngine.Process(bmp).GetText().ToLower();
                    var dungeons = new List<string>();

                    bool isFound = false;

                    foreach (var d in targetDungeons)
                    {
                        if (textData.Contains(d))
                        {
                            isFound |= true;
                            dungeons.Add(d);
                        }
                    }

                    lock (dataLocker)
                    {
                        IsFoundDungeon = isFound;
                        newFoundDungeons = dungeons;
                    }

                    Console.WriteLine(textData);
                }

                Console.WriteLine("");
                Console.WriteLine($"Waiting for {checkDelay} seconds");
                Thread.Sleep(checkDelay * 2000);
            }
        }

        private void SaveSettings()
        {
            var settings = new JObject();
            var checkPosition = new JArray();

            checkPosition.Add(imageCheckPosition.X);
            checkPosition.Add(imageCheckPosition.Y);
            checkPosition.Add(imageCheckPosition.Width);
            checkPosition.Add(imageCheckPosition.Height);

            settings.Add("ImageCheckPosition", checkPosition);

            var dungeonsList = new JArray();

            foreach (var d in targetDungeons)
            {
                dungeonsList.Add(d);
            }

            settings.Add("TargetDungeons", dungeonsList);
            settings.Add("NotifSoundIndex", m_SelectedNotifSoundIndex);

            var path = $"{GetExecutingLocation()}\\config.json";

            File.WriteAllText(path, settings.ToString(Formatting.Indented));
        }

        private void LoadSettings()
        {
            var path = $"{GetExecutingLocation()}\\config.json";

            if (!File.Exists(path))
                return;

            var json = JObject.Parse(File.ReadAllText(path));
            var checkPosition = json["ImageCheckPosition"] as JArray;

            imageCheckPosition.X = (int)checkPosition[0];
            imageCheckPosition.Y = (int)checkPosition[1];
            imageCheckPosition.Width = (int)checkPosition[2];
            imageCheckPosition.Height = (int)checkPosition[3];

            var dungeonsList = json["TargetDungeons"] as JArray;

            var list = new List<string>();

            foreach (var dung in dungeonsList)
            {
                list.Add((string)dung);
            }

            m_SelectedNotifSoundIndex = json.ContainsKey("NotifSoundIndex") 
                ? (int)json["NotifSoundIndex"]
                : 0;

            targetDungeons = list.ToArray();
        }

        private string GetExecutingLocation()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private void DetectNotifSounds()
        {
            var location = GetExecutingLocation();
            var files = Directory.GetFiles(location, "*.wav");

            m_NotifSounds.Clear();

            foreach (var f in files)
            {
                m_NotifSounds.Add(f);
            }
        }

        private void Beep()
        {
            m_NotifPlayer.SoundLocation = m_NotifSounds[m_SelectedNotifSoundIndex];
            m_NotifPlayer.Stop();
            m_NotifPlayer.Play();
        }

        ~Program()
        {

        }
    }
}