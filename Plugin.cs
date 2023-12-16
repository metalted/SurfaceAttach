using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace SurfaceAttach
{
    public class Handle : MonoBehaviour
    {
        public int side;
        public int pIndex;
        public int cIndex;
        public BlockProperties bp;
        public Vector3 transformPoint;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localPos;
        public Vector3 forward;

        public void Set(int side, int pIndex, int cIndex)
        {
            this.side = side;
            this.pIndex = pIndex;
            this.cIndex = cIndex;
        }

        public void Log()
        {
            Debug.LogWarning($"{side},{pIndex},{cIndex}");
        }
    }

    public class SelectedHandle
    {
        public BlockProperties bp;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localPos;

        public int side;
        public int pIndex;
        public int cIndex;

        public SelectedHandle(Handle h)
        {
            bp = h.bp;
            side = h.side;
            pIndex = h.pIndex;
            cIndex = h.cIndex;
            position = h.position;
            rotation = h.rotation;
            localPos = h.localPos;
        }
    }


    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.metalted.zeepkist.surfaceattach";
        public const string pluginName = "Surface Attach";
        public const string pluginVersion = "1.0";
        public float handleSize = 0.5f;
        public static Plugin plg;
        public LEV_LevelEditorCentral central;
        public SelectedHandle selectedHandle = null;       

        public GameObject[] handles;

        private void Awake()
        {
            plg = this;

            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            Color32[] colors = new Color32[] { Color.red, Color.green, Color.blue };
            handles = new GameObject[54];
            int c = 0;
            for(int s = 0; s < 6; s++)
            {
                for(int i = 0; i < 9; i++)
                {
                    handles[c] = GameObject.CreatePrimitive(PrimitiveType.Cube);

                    int axis = Mathf.FloorToInt(s / 2f);
                    Vector3 scale = (Vector3.one * handleSize);
                    scale[axis] = handleSize / 10f;
                    handles[c].transform.localScale = scale;
                    handles[c].GetComponent<Renderer>().material.color = colors[axis];
                    handles[c].SetActive(false);
                    GameObject.DontDestroyOnLoad(handles[c]);
                    Handle h = handles[c].AddComponent<Handle>();
                    h.Set(s, i, c);
                    c++;
                }               
            }
        }

        public void Update()
        {
            if (central != null)
            {
                if (central.cam.IsCursorInGameView())
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (central.click.hoveredBuilding == null)
                        {
                            RaycastHit hitInfo = new RaycastHit();
                            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                            if (Physics.Raycast(ray, out hitInfo, 9999999f))
                            {
                                Handle h = hitInfo.collider.gameObject.GetComponent<Handle>();
                                if(h != null)
                                {
                                    if(Input.GetKey(KeyCode.LeftShift))
                                    {
                                        //This is a placement action
                                        h.Log();

                                        if(selectedHandle != null)
                                        {
                                            if(selectedHandle.bp == null)
                                            {
                                                //BP destroyed
                                                PlayerManager.Instance.messenger.Log("Clear Handle", 1f);
                                                selectedHandle = null;
                                            }
                                            else
                                            {
                                                //Match the selected handle to the current handle.
                                                BlockProperties blockToMove = selectedHandle.bp;
                                                Vector3 newPosition = h.position + h.forward * h.localPos.magnitude;

                                                blockToMove.transform.position = newPosition;
                                                Debug.LogWarning(blockToMove.transform.position);

                                                blockToMove.transform.rotation = h.rotation;
                                                //blockToMove.transform.position = blockToMove.transform.TransformPoint(selectedHandle.localPos);

                                                PlayerManager.Instance.messenger.Log("Clear Handle", 1f);
                                                selectedHandle = null;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //This is a selection action
                                        h.Log();
                                        selectedHandle = new SelectedHandle(h);
                                        PlayerManager.Instance.messenger.Log("Selected Handle", 1f);
                                    }                                    
                                    //Save the blockproperties and the indices i guess.
                                }
                            }
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    if (central.selection.list.Count == 1)
                    {
                        //Debug.LogError(central.selection);
                        //LogError(central.selection.list);
                        //Get the bounding box size
                        BlockProperties bp = central.selection.list[0];
                        //Debug.LogError(bp);
                        Vector3 boundingBoxExtends = Vector3.Scale(bp.boundingBoxSize, bp.transform.localScale) / 2f;
                        Vector3 boundingBoxOffset = Vector3.Scale(bp.boundingBoxOffset, bp.transform.localScale);
                        //Debug.LogError(boundingBoxExtends);
                        int c = 0;
                        for (int s = 0; s < 6; s++)
                        {
                            //Create a copy
                            Vector3 extents = boundingBoxExtends;

                            //Get the currenASt axis (x:1, y:2, z:3)
                            int axis = Mathf.FloorToInt(s / 2f);
                            int[] planeAxis = new int[] { 0, 1, 2 }.Where(p => p != axis).ToArray();
                            int axisX = planeAxis[0];
                            int axisY = planeAxis[1];                            

                            //The center position of the plane                           
                            Vector3 actualCenter = Vector3.zero; 
                            Vector3 center = Vector3.zero;
                            actualCenter[axis] = (extents[axis]) * ((s % 2 == 0) ? 1 : -1);
                            center[axis] = (extents[axis] + handleSize / 2f) * ((s % 2 == 0) ? 1 : -1);

                            //Create a plane of 9 points
                            for (int y = -1; y <= 1; y++)
                            {
                                for (int x = -1; x <= 1; x++)
                                {
                                    Vector3 actualPosition = actualCenter;
                                    actualPosition[axisX] = x * extents[axisX];
                                    actualPosition[axisY] = y * extents[axisY];

                                    Vector3 handlePosition = center;
                                    handlePosition[axisX] = x * extents[axisX];
                                    handlePosition[axisY] = y * extents[axisY];

                                    Vector3 handlePositionP = bp.transform.TransformPoint(handlePosition + boundingBoxOffset);
                                    Vector3 actualPositionP = bp.transform.TransformPoint(actualPosition + boundingBoxOffset);

                                    handles[c].transform.position = handlePositionP;
                                    handles[c].transform.rotation = bp.transform.rotation;
                                    Handle h = handles[c].GetComponent<Handle>();
                                    h.bp = bp;
                                    h.position = actualPositionP;
                                    h.localPos = actualPosition;
                                    h.forward = (h.position - bp.transform.position).normalized;

                                    switch (s)
                                    {
                                        case 0:
                                        case 1:
                                        case 4:
                                        case 5:
                                            h.rotation = Quaternion.LookRotation((h.position - bp.transform.position).normalized, bp.transform.up);                                            
                                            break;
                                        case 2:
                                        case 3:
                                            h.rotation = Quaternion.LookRotation((h.position - bp.transform.position).normalized, bp.transform.forward);                                            
                                            break;                                        
                                    }
                                     

                                    
                                    handles[c].SetActive(true);
                                    c++;
                                    //Debug.LogError(c);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (GameObject handle in handles)
                        {
                            handle.SetActive(false);
                        }
                    }

                }
            }
        }
    }

    [HarmonyPatch(typeof(LEV_LevelEditorCentral), "Awake")]
    public static class LEVAwake
    {
        public static void Postfix(LEV_LevelEditorCentral __instance)
        {
            Plugin.plg.central = __instance;
        }
    }
}
