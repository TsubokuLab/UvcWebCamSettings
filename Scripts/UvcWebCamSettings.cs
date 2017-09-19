using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

public class UvcWebCamSettings : MonoBehaviour
{
	[DllImport ("OpenCvCamera")]
	private static extern IntPtr GetCamera(int device, int width = 640, int height = 480, int fps = 30, string codec = "MJPG");
	[DllImport ("OpenCvCamera")]
	private static extern void ReleaseCamera(IntPtr ptr);
	[DllImport ("OpenCvCamera")]
	private static extern int GetCameraWidth(IntPtr ptr);
	[DllImport ("OpenCvCamera")]
	private static extern int GetCameraHeight(IntPtr ptr);
	[DllImport ("OpenCvCamera")]
	private static extern int GetCameraFps(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraCodec(IntPtr ptr, string codec = "MJPG");
	[DllImport ("OpenCvCamera")]
	private static extern void SetCameraTexturePtr(IntPtr ptr, IntPtr texture);
	[DllImport ("OpenCvCamera")]
	private static extern IntPtr GetRenderEventFunc();

    [DllImport("OpenCvCamera")]
    private static extern void SetCameraHue(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraHue(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraSaturation(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraSaturation(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraBrightness(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraBrightness(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraContrast(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraContrast(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraGain(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraGain(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraExposure(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraExposure(IntPtr ptr);
    [DllImport("OpenCvCamera")]
    private static extern void SetCameraFocus(IntPtr ptr, double _value = 0.0f);
    [DllImport("OpenCvCamera")]
    private static extern double GetCameraFocus(IntPtr ptr);

    public Material WebCamMaterial;
	private IntPtr camera_ = IntPtr.Zero;

    [Header("----- Cameras -----")]
    public string[] deviceNameList;
    private WebCamDevice[] devices;
    [Button("updateDeviceList", "Update Camera List")]
    public int updateDeviceListButton;
    private string[] CodecList =
    {
        "MJPG",
        "MPG4",
        "MP42",
        "DIV3",
        "DIVX",
        "PIM1",
        "U263",
        "I263",
        "H264",
        "FLV1",
        "YUY2",
        "YUYU",
        "YUYV",
        "IYUV",
        "UYVY"
    };
    private int codecNum = 0;

    //[Button("Pause", "Pause")]
    //public int PauseButton;
    //[Button("Resume", "Resume")]
    //public int ResumeButton;
    
    [Header("----- Window -----")]
    public bool showWindow = false;
    public KeyCode toggleWindowKey = KeyCode.F1;
    public Rect configWindowRect = new Rect(0, 0, 400, 400);
    private Vector2 scrollPosition = Vector2.zero;

    [Header("----- Settings -----")]
    public OpenCvCameraSettings settings;
    public string fileName = "camera_settings.xml";
    private bool bSaveSettings;
    private bool bLoadSettings;
    [Button("SaveSettings", "SaveSettings")]
    public int SaveSettingsButton;
    [Button("LoadSettings", "LoadSettings")]
    public int LoadSettingsButton;
    private bool bDevices = false;
    private bool bResolution = false;
    private bool bVideoPropAmp = false;
    private bool bCameraControl = false;

    private Coroutine renderCoroutine;

    void Start()
	{
        updateDeviceList();

        if (LoadSettings())
        {
            OpenCamera();
        }
        else
        {
            settings.cameraNum = 0;
            settings.width = 640;
            settings.height = 480;
            settings.fps = 30;
            settings.codec = "MJPG";
            settings.hue = 0;
            settings.saturation = 50;
            settings.brightness = 64;
            settings.gain = 0;
            settings.exposure = -4;
            settings.focus = 0;
        }
        
    }

    void OnGUI()
    {
        if (showWindow)
        {
            configWindowRect = GUI.Window((int)toggleWindowKey, configWindowRect, DoMyWindow, "<< ----- UVC WebCam Settings [ " + toggleWindowKey.ToString() + " ] ----- >>");
        }
    }

    void DoMyWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

        GUI.BeginGroup(new Rect(0, 0, configWindowRect.width, configWindowRect.height));
        GUILayout.BeginArea(new Rect(10, 20, configWindowRect.width - 20, configWindowRect.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        if (bDevices)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(20))) bDevices = !bDevices;
            GUILayout.Label("[ Devices ]", GUILayout.Width(140));
            GUILayout.EndHorizontal();
            if (settings.cameraNum < devices.Length) GUILayout.Label("[ " + settings.cameraNum + " ] : " + devices[settings.cameraNum].name);
            int _num = GUILayout.SelectionGrid(settings.cameraNum, deviceNameList, 1);
            if (settings.cameraNum != _num)
            {
                settings.cameraNum = _num;
                OpenCamera();
            }
        }else
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20))) bDevices = !bDevices;
            GUILayout.Label("[ Devices ]", GUILayout.Width(140));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (bResolution)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(20))) bResolution = !bResolution;
            GUILayout.Label("[ Resolution ]");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Width : ", GUILayout.Width(60));
            settings.width = int.Parse(GUILayout.TextArea(settings.width.ToString(), GUILayout.Width(60)));
            GUILayout.Label("px", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hieght : ", GUILayout.Width(60));
            settings.height = int.Parse(GUILayout.TextArea(settings.height.ToString(), GUILayout.Width(60)));
            GUILayout.Label("px", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Fps : ", GUILayout.Width(60));
            settings.fps = int.Parse(GUILayout.TextArea(settings.fps.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Codec : ", GUILayout.Width(60));
            codecNum = GUILayout.SelectionGrid(codecNum, CodecList, 5);
            settings.codec = CodecList[codecNum];
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Set", GUILayout.Height(40)))
            {
                OpenCamera();
            }
        }else
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20))) bResolution = !bResolution;
            GUILayout.Label("[ Resolution ]");
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (bVideoPropAmp)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(20))) bVideoPropAmp = !bVideoPropAmp;
            GUILayout.Label("[ Video Proc Amp ]");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Brightness : ", GUILayout.Width(80));
            int _b = (int)GUILayout.HorizontalSlider((float)settings.brightness,0, 255);
            GUILayout.Label(settings.brightness.ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.brightness != _b)
            {
                settings.brightness = _b;
                SetCameraBrightness(camera_, settings.brightness);
                Debug.Log("Brightness : " + GetCameraBrightness(camera_));
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Contrast : ", GUILayout.Width(80));
            int _c = (int)GUILayout.HorizontalSlider((float)settings.contrast, 0, 255);
            GUILayout.Label((settings.contrast).ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.contrast != _c)
            {
                settings.contrast = _c;
                SetCameraContrast(camera_, settings.contrast);
                Debug.Log("Contrast : " + GetCameraContrast(camera_));
            }

            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hue : ", GUILayout.Width(80));
            int _h = (int)GUILayout.HorizontalSlider((float)settings.hue, 0, 100);
            GUILayout.Label((settings.hue).ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.hue != _h)
            {
                settings.hue = _h;
                SetCameraHue(camera_, settings.hue);
                Debug.Log("Hue : " + GetCameraHue(camera_));
            }
            */

            GUILayout.BeginHorizontal();
            GUILayout.Label("Saturation : ", GUILayout.Width(80));
            int _s = (int)GUILayout.HorizontalSlider((float)settings.saturation, 0, 100);
            GUILayout.Label((settings.saturation).ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.saturation != _s)
            {
                settings.saturation = _s;
                SetCameraSaturation(camera_, settings.saturation);
                Debug.Log("Saturation : " + GetCameraSaturation(camera_));
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Exposure : ", GUILayout.Width(80));
            int _e = (int)GUILayout.HorizontalSlider((float)settings.exposure, -8, 0);
            GUILayout.Label((settings.exposure).ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.exposure != _e)
            {
                settings.exposure = _e;
                SetCameraExposure(camera_, settings.exposure);
                Debug.Log("Exposure : " + GetCameraExposure(camera_));
            }

            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label("Gain : ", GUILayout.Width(80));
            int _g = (int)GUILayout.HorizontalSlider((float)settings.gain, 0, 100);
            GUILayout.Label(settings.gain.ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.gain != _g)
            {
                settings.gain = _g;
                SetCameraGain(camera_, settings.gain);
                Debug.Log("Gain : " + GetCameraGain(camera_));
            }
            */
        }else
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20))) bVideoPropAmp = !bVideoPropAmp;
            GUILayout.Label("[ Video Proc Amp ]");
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (bCameraControl)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(20))) bCameraControl = !bCameraControl;
            GUILayout.Label("[ Camera Control ]");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Focus : ", GUILayout.Width(80));
            int _f = (int)GUILayout.HorizontalSlider((float)settings.focus, 0, 100);
            GUILayout.Label(settings.focus.ToString("f0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (settings.focus != _f)
            {
                settings.focus = _f;
                SetCameraFocus(camera_, settings.focus);
                Debug.Log("Focus : " + GetCameraFocus(camera_));
            }
        }else
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20))) bCameraControl = !bCameraControl;
            GUILayout.Label("[ Camera Control ]");
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(40)))
        {
            SaveSettings();
        }
        if (GUILayout.Button("Load", GUILayout.Height(40)))
        {
            LoadSettings();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.EndArea();
        GUI.EndGroup();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleWindowKey))
        {
            toggleMenu();
        }
    }

    public void OpenCamera()
    {
        if (settings.cameraNum >= devices.Length) {
            Debug.LogWarning("[ UvcWebCamSettings ] Camera[ " + settings.cameraNum + " ] is not exit.");
            return;
        }
        if (renderCoroutine != null) {
            OnDestroy();
        }

        Debug.Log("[ UvcWebCamSettings ] OpenCamera( " + settings.cameraNum + ", " + settings.width + ", " + settings.height + ", " + settings.fps + ", " + settings.codec + ")");
        camera_ = GetCamera(settings.cameraNum, settings.width, settings.height, settings.fps, settings.codec);
        if (camera_ != IntPtr.Zero)
        {
            Debug.Log("[ UvcWebCamSettings ] Camera[ " + settings.cameraNum + " ] : " + devices[settings.cameraNum].name + " open. ( " + GetCameraWidth(camera_) + ", " + GetCameraHeight(camera_) + " : " + GetCameraFps(camera_) + ")");
        }
        else
        {
            Debug.Log("[ UvcWebCamSettings ] Camera[ " + settings.cameraNum + " ] : " + devices[settings.cameraNum].name + ", couldn't open.");
        }

        var tex = new Texture2D(
        GetCameraWidth(camera_),
        GetCameraHeight(camera_),
        TextureFormat.ARGB32,
        false);

        if (!WebCamMaterial) WebCamMaterial = gameObject.GetComponent<Renderer>().sharedMaterial;
        WebCamMaterial.mainTexture = tex;

        SetCameraTexturePtr(camera_, tex.GetNativeTexturePtr());

        renderCoroutine = StartCoroutine(OnRender());
    }

    public void Pause()
    {
        if (renderCoroutine != null)
        {
            Debug.Log("[ UvcWebCamSettings ] Pause");
            StopCoroutine(renderCoroutine);
        }
        renderCoroutine = null;
    }

    public void Resume()
    {
        if (renderCoroutine == null)
        {
            Debug.Log("[ UvcWebCamSettings ] Resume");
            renderCoroutine = StartCoroutine(OnRender());
        }
    }

    void OnDestroy()
	{
        if(renderCoroutine != null) StopCoroutine(renderCoroutine);
        renderCoroutine = null;

        ReleaseCamera(camera_);
        camera_ = IntPtr.Zero;
        Debug.Log("[ UvcWebCamSettings ] Release");
	}

    void updateDeviceList()
    {
        devices = WebCamTexture.devices;
        deviceNameList = new string[devices.Length];
        // display all cameras
        for (var i = 0; i < devices.Length; i++)
        {
            Debug.Log("[ UvcWebCamSettings ] Camera[ " + i + " ] : " + devices[i].name);
            deviceNameList[i] = devices[i].name;
        }
    }

    IEnumerator OnRender()
    {
        for (;;)
        {
            yield return new WaitForEndOfFrame();
            GL.IssuePluginEvent(GetRenderEventFunc(), 0);
        }
    }

    public bool SaveSettings()
    {
        Debug.Log("[ UvcWebCamSettings ] SaveSettings( " + fileName + " )");

        var serializer = new XmlSerializer(typeof(OpenCvCameraSettings));
        using (var stream = new FileStream(fileName, FileMode.Create))
        {
            serializer.Serialize(stream, settings);
        }
        return true;
    }

    public bool LoadSettings()
    {
        Debug.Log("[ UvcWebCamSettings ] LoadSettings( " + fileName + " )");

        if (!System.IO.File.Exists(fileName))
        {
            Debug.LogWarning("[ UvcWebCamSettings ] " + fileName + " is not exists.");
            return false;
        }
        var serializer = new XmlSerializer(typeof(OpenCvCameraSettings));
        using (var stream = new FileStream(fileName, FileMode.Open))
        {
            settings = (OpenCvCameraSettings)serializer.Deserialize(stream);
        }

        // set
        /*
        SetCameraBrightness(camera_, settings.brightness);
        Debug.Log("Brightness : " + GetCameraBrightness(camera_));
        SetCameraContrast(camera_, settings.contrast);
        Debug.Log("Contrast : " + GetCameraContrast(camera_));
        SetCameraSaturation(camera_, settings.saturation);
        Debug.Log("Saturation : " + GetCameraSaturation(camera_));
        SetCameraExposure(camera_, settings.exposure);
        Debug.Log("Exposure : " + GetCameraExposure(camera_));
        */

        for (int i = 0; i < CodecList.Length; i++)
        {
            if (CodecList[i] == settings.codec)
            {
                codecNum = i;
                return true;
            }
        }
        Debug.LogWarning("[ UvcWebCamSettings ] codec " + settings.codec + " is not exists.");
        return false;
    }

    public void toggleMenu()
    {
        showWindow = !showWindow;
    }
}

// Camera Settings
[System.Serializable]
public struct OpenCvCameraSettings
{
    public int cameraNum;
    public int width;
    public int height;
    public int fps;
    public string codec;
    public double hue;
    public double saturation;
    public double brightness;
    public double contrast;
    public double exposure;
    public double gain;
    public double focus;
}