using System;
using System.IO;
using System.Reflection;
using MSCLoader;
using UnityEngine;

namespace Headlamp
{
    public class HeadlampMod : Mod
    {
        public override string ID => "Headlamp";
        public override string Name => "Headlamp";
        public override string Author => "c0mparn";
        public override string Version => "1.3.0";
        public override Game SupportedGames => Game.MyWinterCar;

        // Keybind
        private static SettingsKeybind KeyToggle;

        // Headlamp state
        private bool isHeadlampOn = false;
        private GameObject headlampLight;
        private Light spotLight;
        private Texture2D cookieTexture;
        private Texture2D userTexture;
        
        // Settings
        private const float LightRange = 35f;
        private const float LightIntensity = 1.5f;
        private const float SpotAngle = 90f;
        private static readonly Color LightColor = new Color(1f, 0.95f, 0.8f);
        
        // Cookie settings
        private const int CookieSize = 256;
        private const float FeatherStart = 0.3f;
        private const float FeatherEnd = 0.95f;
        
        // Get Mods directory path
        private string ModsDirectory
        {
            get
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(assemblyPath);
            }
        }
        
        private string CookiePath => Path.Combine(ModsDirectory, "headlamp_cookie.png");
        
        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, OnLoad);
            SetupFunction(Setup.Update, OnUpdate);
            SetupFunction(Setup.ModSettings, ModSettings);
        }
        
        private void ModSettings()
        {
            KeyToggle = Keybind.Add("ToggleHeadlamp", "Toggle Headlamp", KeyCode.G);
        }
        
        private void OnLoad()
        {
            ModConsole.Print("Headlamp mod v1.3.0 loaded! Press G to toggle headlamp.");
            
            LoadUserTexture();
            CreateCookieTexture();
        }

        private void LoadUserTexture()
        {
            if (!File.Exists(CookiePath))
            {
                ModConsole.Print("No custom pattern texture found. Using default feathered circle.");
                return;
            }
            
            try
            {
                byte[] imageData = File.ReadAllBytes(CookiePath);
                userTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                
                if (userTexture.LoadImage(imageData))
                {
                    ModConsole.Print("Custom pattern texture loaded!");
                }
                else
                {
                    userTexture = null;
                }
            }
            catch (Exception ex)
            {
                ModConsole.Print($"Warning: Could not load pattern texture: {ex.Message}");
                userTexture = null;
            }
        }

        private void CreateCookieTexture()
        {
            cookieTexture = new Texture2D(CookieSize, CookieSize, TextureFormat.Alpha8, false);
            cookieTexture.wrapMode = TextureWrapMode.Clamp;
            
            float center = CookieSize / 2f;
            float maxRadius = CookieSize / 2f;
            
            Color[] pixels = new Color[CookieSize * CookieSize];
            
            for (int y = 0; y < CookieSize; y++)
            {
                for (int x = 0; x < CookieSize; x++)
                {
                    float dx = (x - center) / maxRadius;
                    float dy = (y - center) / maxRadius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    float alpha;
                    if (distance <= FeatherStart)
                    {
                        alpha = 1f;
                    }
                    else if (distance >= FeatherEnd)
                    {
                        alpha = 0f;
                    }
                    else
                    {
                        float t = (distance - FeatherStart) / (FeatherEnd - FeatherStart);
                        alpha = 1f - (t * t * (3f - 2f * t));
                    }
                    
                    if (userTexture != null)
                    {
                        float u = (float)x / CookieSize;
                        float v = (float)y / CookieSize;
                        Color userPixel = userTexture.GetPixelBilinear(u, v);
                        float userValue = (userPixel.r + userPixel.g + userPixel.b) / 3f;
                        alpha *= userValue;
                    }
                    
                    pixels[y * CookieSize + x] = new Color(alpha, alpha, alpha, alpha);
                }
            }
            
            cookieTexture.SetPixels(pixels);
            cookieTexture.Apply();
            
            ModConsole.Print("Cookie texture created with feathered edges!");
        }

        private void OnUpdate()
        {
            if (KeyToggle != null && KeyToggle.GetKeybindDown())
            {
                ToggleHeadlamp();
            }
            
            if (isHeadlampOn && headlampLight != null)
            {
                UpdateHeadlampPosition();
            }
        }

        private void ToggleHeadlamp()
        {
            isHeadlampOn = !isHeadlampOn;
            
            if (isHeadlampOn)
            {
                CreateHeadlamp();
                ModConsole.Print("Headlamp ON");
            }
            else
            {
                DestroyHeadlamp();
                ModConsole.Print("Headlamp OFF");
            }
        }

        private void CreateHeadlamp()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                ModConsole.Print("Warning: Could not find main camera!");
                isHeadlampOn = false;
                return;
            }
            
            headlampLight = new GameObject("PlayerHeadlamp");
            
            spotLight = headlampLight.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = LightColor;
            spotLight.intensity = LightIntensity;
            spotLight.range = LightRange;
            spotLight.spotAngle = SpotAngle;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowStrength = 0.8f;
            
            if (cookieTexture != null)
            {
                spotLight.cookie = cookieTexture;
            }
            
            UpdateHeadlampPosition();
            
            GameObject.DontDestroyOnLoad(headlampLight);
        }

        private void UpdateHeadlampPosition()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null || headlampLight == null) return;
            
            headlampLight.transform.position = mainCam.transform.position;
            headlampLight.transform.rotation = mainCam.transform.rotation;
        }

        private void DestroyHeadlamp()
        {
            if (headlampLight != null)
            {
                GameObject.Destroy(headlampLight);
                headlampLight = null;
                spotLight = null;
            }
        }
    }
}
