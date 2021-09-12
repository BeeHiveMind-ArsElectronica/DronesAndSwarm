using UnityEngine;
using System.Collections;

public class DroneObjectBhv : MonoBehaviour
{

    public int droneId;

    Color diffusorColorLast;
    public Color diffusorColor;
    public Color bodyColor;

    public GcTypes.ObjectInfo objectInfo;
    public GcTypes.VideoInfo videoInfo;

    public GameObject modelSpaxel;
    public MeshRenderer spaxelDiffusor;
    public GameObject modelGround;
    public GameObject modelFlyingscreen;

    public GameObject curModel;
    public GameObject Quad;

    public GameObject typeIndicator;
    public bool selected = false;

    // Use this for initialization
    void Start()
    {
        diffusorColor = diffusorColorLast = Color.black;

        videoInfo = new GcTypes.VideoInfo();
        curModel = modelSpaxel;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(
            objectInfo.trackingX,
            objectInfo.trackingZ,
            objectInfo.trackingY);

        Quaternion q = new Quaternion(
            objectInfo.trackingOrientationX,
            objectInfo.trackingOrientationZ,
            objectInfo.trackingOrientationY,
            -objectInfo.trackingOrientationW);

        transform.localRotation = q;//q.eulerAngles;

        if (modelSpaxel.activeSelf)
        {
            spaxelDiffusor.material.color =
                new Color(objectInfo.colorR, objectInfo.colorG, objectInfo.colorB);


            if (diffusorColor != diffusorColorLast)
            {
                var vcolor = new Vector3(diffusorColor.r, diffusorColor.g, diffusorColor.b);
                TcpMgr.Instance.CmdExtColor(Util.SingleIdToSet(droneId), vcolor, 0.0f);
                diffusorColorLast = diffusorColor;
            }

        }

        bool active = (objectInfo.flags & (uint)GcTypes.ObjectInfoFlags.FLAG_ACTIVE) != 0;

        //Debug.Log(objectInfo.id + " a: " + active);

        if ((GcTypes.DroneType)objectInfo.type == GcTypes.DroneType.UGV
            && !modelGround.activeSelf)
        {
            modelSpaxel.SetActive(false);
            modelFlyingscreen.SetActive(false);
            modelGround.SetActive(active);
            curModel = modelGround;
            bodyColor = curModel.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material.color;
        }

        if ((GcTypes.DroneType)objectInfo.type == GcTypes.DroneType.UAV
            && !modelSpaxel.activeSelf)
        {
            modelSpaxel.SetActive(active);
            modelFlyingscreen.SetActive(false);
            modelGround.SetActive(false);
            curModel = modelSpaxel;
            // TODO: Get rid of GetChild(0).GetChild(0) ...
            if (curModel.transform.GetChild(0).GetChild(0).childCount <= 0)
            {
                return;
            }
            bodyColor = curModel.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material.color;
        }

        if (!active && curModel.activeSelf)
        {
            curModel.SetActive(false);
        }
        else if (active && !curModel.activeSelf)
        {
            curModel.SetActive(true);
        }

        Color c = Color.white;
        switch (objectInfo.liveness)
        {
            case 0: c = Color.white; break;
            case 1: c = Color.red; break;
            case 2: c = Color.blue; break;
        }

        if (Main.Instance.DroneLabelInfo.ContainsKey(droneId))
        {
            Quad.transform.localScale = new Vector3(Quad.transform.localScale.x, Mathf.Min(1.0f, float.Parse(Main.Instance.DroneLabelInfo[droneId])), Quad.transform.localScale.z);
        }
        else
        {
            Quad.transform.localScale = new Vector3(Quad.transform.localScale.x, 0, Quad.transform.localScale.z);
        }


        typeIndicator.GetComponent<MeshRenderer>().material.color = c;
        UpdateCollider();
    }

    void UpdateCollider()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            return;
        }
        if (curModel.transform.GetChild(0).GetChild(0).childCount <= 0)
        {
            return;
        }

        curModel.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material.color = (selected ? Color.yellow : bodyColor);
        if (SceneManager.Instance != null)
        {
            if (SceneManager.Instance.selectionEnabled)
            {
                collider.enabled = true;
                return;
            }
        }
        selected = false;
        collider.enabled = false;
    }

    void OnPreRender()
    {
        GL.wireframe = true;
    }
    void OnPostRender()
    {
        GL.wireframe = false;
    }

    void OnMouseDown()
    {
        selected = !selected;
    }
}