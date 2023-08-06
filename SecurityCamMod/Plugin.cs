using BepInEx;
using GorillaLocomotion.Gameplay;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.XR;
using Utilla;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace SecurityCamMod
{
    /// <summary>
    /// This is your mod's main class.
    /// </summary>

    /* This attribute tells Utilla to look for [ModdedGameJoin] and [ModdedGameLeave] */
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom = false;

        public AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            return bundle;
        }

        GameObject asset = null;

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        void OnEnable()
        {
            /* Set up your mod here */
            /* Code here runs at the start and whenever your mod is enabled*/

            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            /* Undo mod setup here */
            /* This provides support for toggling mods with ComputerInterface, please implement it :) */
            /* Code here runs whenever your mod is disabled (including if it disabled on startup)*/
            asset.SetActive(false);
            MainAsset.SetActive(false);
            cameras[0].SetActive(false);
            GameObject.Find("CM vcam1").SetActive(true);
            GameObject.Find("Shoulder Camera").GetComponent<Camera>().depth = 2;
            HarmonyPatches.RemoveHarmonyPatches();
        }

        GameObject MainAsset;

        static bool isGripPressed = false;

        void OnGameInitialized(object sender, EventArgs e)
        {
            AssetBundle bundle = LoadAssetBundle("SecurityCamMod.securityassets.securitycam");
            asset = bundle.LoadAsset<GameObject>("camera");
            MainAsset = Instantiate(asset);
            MainAsset.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.position;
            MainAsset.AddComponent<CollisionDetector>();
            MainAsset.AddComponent<Rigidbody>().useGravity = false;
        }

        private int gripPressCount = 0;

        private GameObject[] cameras = new GameObject[1];

        void Update()
        {
            try
            {
                if (inRoom)
                {
                    int i = 30;
                    MainAsset.SetActive(true);
                    MainAsset.transform.parent = GorillaLocomotion.Player.Instance.rightControllerTransform;
                    MainAsset.transform.localPosition = new Vector3(0.1f, 0f, 0f);
                    MainAsset.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    MainAsset.transform.rotation = GorillaLocomotion.Player.Instance.rightControllerTransform.rotation * Quaternion.Euler(90f, 90f, 0f);

                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.gripButton, out bool d);
                    Vector3 direction = MainAsset.transform.right;
                    Ray ray = new Ray(GorillaLocomotion.Player.Instance.rightControllerTransform.position, direction);
                    int layerMask = 1 << i;
                    bool isColliding = Physics.Raycast(ray, out RaycastHit hit, 0.32f, ~layerMask);
                    if (pointer == null)
                    {
                        pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        UnityEngine.Object.Destroy(pointer.GetComponent<Rigidbody>());
                        UnityEngine.Object.Destroy(pointer.GetComponent<SphereCollider>());
                        pointer.transform.localScale = new Vector3(0.2f, 0.00000000000000000000000000001f, 0.2f);
                        pointer.GetComponent<Renderer>().material.color = Color.blue;
                    }
                    pointer.layer = i;
                    MainAsset.layer = i;
                    GorillaTagger.Instance.rightHandTransform.gameObject.layer = i;
                    pointer.transform.position = hit.point;
                    pointer.transform.rotation = GorillaLocomotion.Player.Instance.rightControllerTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                    if (d && !isGripPressed)
                    {
                        isGripPressed = true;
                        if (isColliding)
                        {
                            if (cameras[gripPressCount] != null)
                            {
                                Destroy(cameras[gripPressCount]);
                            }
                            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 d2);
                            float rotationSpeed = 50.0f;
                            cameras[gripPressCount] = Instantiate(asset);
                            cameras[gripPressCount].SetActive(true);
                            Vector3 cameraOffset = new Vector3(0f, -3.5f, 0f);
                            cameras[gripPressCount].transform.position = MainAsset.transform.position;
                            cameras[gripPressCount].transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                            Debug.Log("New Camera Placed");
                            gripPressCount = (gripPressCount + 1) % 1;
                            UpdateCameras();
                            cameras[gripPressCount].transform.rotation = MainAsset.transform.rotation;
                        }
                    }
                    else if (!d)
                    {
                        isGripPressed = false;
                    }
                }
                else
                {
                    asset.SetActive(false);
                    MainAsset.SetActive(false);
                    cameras[0].SetActive(false);
                    GameObject.Find("CM vcam1").SetActive(true);
                    GameObject.Find("Shoulder Camera").GetComponent<Camera>().depth = 2;
                }
            }
            catch (Exception e)
            {
                
            }
        }

        GameObject cameraManager = null;

        public GameObject[] cameraObjects = new GameObject[3];

        Quaternion reference = Quaternion.identity;

        void UpdateCameras()
        {
            cameraManager = new GameObject("Camera Manager");
            cameraManager.transform.position = MainAsset.transform.position;
            cameraManager.transform.rotation = GorillaLocomotion.Player.Instance.rightControllerTransform.rotation;
            cameraManager.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            GameObject shoulderCamera = GameObject.Find("Shoulder Camera");
            if (shoulderCamera == null)
            {
                Debug.LogError("Shoulder Camera not found");
                return;
            }

            GameObject cmvc = GameObject.Find("CM vcam1");
            if (cmvc != null)
            {
                cmvc.SetActive(false);
            }

            Camera mainCamera = shoulderCamera.GetComponent<Camera>();
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            mainCamera.depth = -1;

            int cameraCount = cameras.Length;

            for (int i = 0; i < cameraCount; i++)
            {
                if (cameras[i] != null)
                {
                    Camera camera = cameras[0].GetComponentInChildren<Camera>();
                    if (camera == null)
                    {
                        GameObject cameraChild = new GameObject("CameraChild");
                        cameraChild.transform.SetParent(cameras[0].transform);
                        cameraChild.transform.localPosition = Vector3.zero;
                        cameraChild.transform.localRotation = Quaternion.identity;
                        camera = cameraChild.AddComponent<Camera>();
                    }
                    camera.rect = new Rect(0.6f, 0.5f, 0.25f, 0.25f);
                    camera.depth = 1;
                    camera.clearFlags = CameraClearFlags.Depth;
                    camera.cameraType = CameraType.Game;
                    camera.usePhysicalProperties = false;
                    camera.nearClipPlane = 0.01f;
                    camera.farClipPlane = 1000f;
                    camera.allowHDR = true;
                    camera.allowMSAA = true;
                    camera.allowDynamicResolution = true;
                    reference = camera.transform.rotation;
                    Debug.Log("Cameras rotation: " + reference);
                }
            }
        }


        GameObject pointer = null;

        /* This attribute tells Utilla to call this method when a modded room is joined */
        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            /* Activate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            inRoom = true;
        }

        /* This attribute tells Utilla to call this method when a modded room is left */
        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            /* Deactivate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            inRoom = false;
        }
    }
}
