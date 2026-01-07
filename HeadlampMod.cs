using System;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Headlamp.HeadlampMod), "Headlamp", "1.2.0", "Player")]
[assembly: MelonGame("Aapeli Games", "My Winter Car")]

namespace Headlamp
{
    public class HeadlampMod : MelonMod
    {
        // Headlamp state
        private bool isHeadlampOn = false;
        private GameObject headlampLight;
        private Light spotLight;
        private Texture2D cookieTexture;
        private Texture2D userTexture;
        
        // Settings
        private const KeyCode ToggleKey = KeyCode.G;
        private const float LightRange = 35f;      // Increased range
        private const float LightIntensity = 1.5f;
        private const float SpotAngle = 90f;       // Wider cone
        private static readonly Color LightColor = new Color(1f, 0.95f, 0.8f); // Warm white
        
        // Cookie settings
        private const int CookieSize = 256;
        private const float FeatherStart = 0.3f;  // Where feathering begins (0-1 from center)
        private const float FeatherEnd = 0.95f;   // Where light fully fades out
        
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
        
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Headlamp mod v1.2.0 loaded! Press G to toggle headlamp.");
            
            // Try to load user texture for pattern overlay
            LoadUserTexture();
            
            // Create the cookie texture with proper feathering
            CreateCookieTexture();
        }

        private void LoadUserTexture()
        {
            if (!File.Exists(CookiePath))
            {
                LoggerInstance.Msg("No custom pattern texture found. Using default feathered circle.");
                return;
            }
            
            try
            {
                byte[] imageData = File.ReadAllBytes(CookiePath);
                userTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                
                if (userTexture.LoadImage(imageData))
                {
                    LoggerInstance.Msg("Custom pattern texture loaded!");
                }
                else
                {
                    userTexture = null;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not load pattern texture: {ex.Message}");
                userTexture = null;
            }
        }

        private void CreateCookieTexture()
        {
            // Create a square texture for the cookie
            cookieTexture = new Texture2D(CookieSize, CookieSize, TextureFormat.Alpha8, false);
            cookieTexture.wrapMode = TextureWrapMode.Clamp;
            
            float center = CookieSize / 2f;
            float maxRadius = CookieSize / 2f;
            
            Color[] pixels = new Color[CookieSize * CookieSize];
            
            for (int y = 0; y < CookieSize; y++)
            {
                for (int x = 0; x < CookieSize; x++)
                {
                    // Calculate distance from center (0 to 1)
                    float dx = (x - center) / maxRadius;
                    float dy = (y - center) / maxRadius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    // Calculate alpha with smooth feathering
                    float alpha;
                    if (distance <= FeatherStart)
                    {
                        // Full brightness in center
                        alpha = 1f;
                    }
                    else if (distance >= FeatherEnd)
                    {
                        // Fully faded at edge
                        alpha = 0f;
                    }
                    else
                    {
                        // Smooth falloff using smoothstep
                        float t = (distance - FeatherStart) / (FeatherEnd - FeatherStart);
                        // Smoothstep for natural falloff
                        alpha = 1f - (t * t * (3f - 2f * t));
                    }
                    
                    // Blend with user texture pattern if available
                    if (userTexture != null)
                    {
                        // Sample user texture
                        float u = (float)x / CookieSize;
                        float v = (float)y / CookieSize;
                        Color userPixel = userTexture.GetPixelBilinear(u, v);
                        // Use the luminance of the user texture to modulate
                        float userValue = (userPixel.r + userPixel.g + userPixel.b) / 3f;
                        alpha *= userValue;
                    }
                    
                    pixels[y * CookieSize + x] = new Color(alpha, alpha, alpha, alpha);
                }
            }
            
            cookieTexture.SetPixels(pixels);
            cookieTexture.Apply();
            
            LoggerInstance.Msg("Cookie texture created with feathered edges!");
        }

        public override void OnUpdate()
        {
            // Check for toggle key press
            if (Input.GetKeyDown(ToggleKey))
            {
                ToggleHeadlamp();
            }
            
            // Update headlamp position to follow player camera
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
                LoggerInstance.Msg("Headlamp ON");
            }
            else
            {
                DestroyHeadlamp();
                LoggerInstance.Msg("Headlamp OFF");
            }
        }

        private void CreateHeadlamp()
        {
            // Find the main camera (player's view)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                LoggerInstance.Warning("Could not find main camera!");
                isHeadlampOn = false;
                return;
            }
            
            // Create the headlamp GameObject
            headlampLight = new GameObject("PlayerHeadlamp");
            
            // Add spotlight component
            spotLight = headlampLight.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = LightColor;
            spotLight.intensity = LightIntensity;
            spotLight.range = LightRange;
            spotLight.spotAngle = SpotAngle;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowStrength = 0.8f;
            
            // Apply procedural cookie texture
            if (cookieTexture != null)
            {
                spotLight.cookie = cookieTexture;
            }
            
            // Initial position update
            UpdateHeadlampPosition();
            
            // Prevent destruction on scene change
            GameObject.DontDestroyOnLoad(headlampLight);
        }

        private void UpdateHeadlampPosition()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null || headlampLight == null) return;
            
            // Position the light at the camera position, pointing forward
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

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Recreate headlamp if it was on when scene changed
            if (isHeadlampOn && headlampLight == null)
            {
                CreateHeadlamp();
            }
        }

        public override void OnApplicationQuit()
        {
            DestroyHeadlamp();
        }
    }
}
