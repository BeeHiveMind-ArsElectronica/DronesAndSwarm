 //WASD to orbit, left Ctrl/Alt to zoom
using UnityEngine;

[AddComponentMenu("Camera-Control/Keyboard Orbit")]

public class KeyboardOrbit : MonoBehaviour
{
    public Transform target;
    public float distance = 0.0f;
    public float zoomSpd = 2.0f;
    public float strafeSpd = 2.0f;

    public float xSpeed = 240.0f;
    public float ySpeed = 123.0f;

    public int yMinLimit = -723;
    public int yMaxLimit = 877;

    public float maxOrthoSize = 30.0f;
    public float minOrthoSize = 0.2f;


    private float x = 0.0f;
    private float y = 0.0f;

    private float lastmx = 0;
    private float lastmy = 0;

    private float lastnx = 0;
    private float lastny = 0;

    private bool strf = false;

    private Vector3 m_initPositoin;
    private Vector3 m_initEulerAngles;

    private bool didSnapRotation = false;

    private Vector3 storedEulerAngles;
    private Vector3 storedPosition;

    public void Start ()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Make the rigid body not change rotation
        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;

        m_initPositoin = transform.position;
        m_initEulerAngles = transform.eulerAngles;

        storedPosition = transform.position;
        storedEulerAngles = transform.eulerAngles;
    }

    public void LateUpdate ()
    {
        if (target)
        {
            if (Input.GetKeyDown(KeyCode.O)
                && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                Camera.main.orthographic = !Camera.main.orthographic;

                if (Camera.main.orthographic)
                {
                    storedPosition = transform.position;
                    // set ortho cam 200m above zero to prevent clipping of objects
                    transform.position = Vector3.up * 200;
                }
                else
                {
                    Vector3 eulerAngles = transform.eulerAngles;
                    transform.eulerAngles = storedEulerAngles;
                    storedEulerAngles = eulerAngles;

                    Vector3 position = transform.position;
                    transform.position = storedPosition;
                    storedPosition = position;
                }
            }
            else if(Input.GetKeyDown(KeyCode.Home))
            {
                if (Camera.main.orthographic)
                {
                    transform.position = Vector3.up * 200;
                    transform.rotation = Quaternion.identity;
                }
                else
                {
                    transform.position = m_initPositoin;
                    x = m_initEulerAngles.y;
                    y = m_initEulerAngles.x;
                }
            }
            else if(Input.GetKey(KeyCode.LeftControl)
                || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    transform.position = m_initPositoin;
                    x = m_initEulerAngles.y;
                    y = m_initEulerAngles.x;
                }
            }

            if (Camera.main.orthographic)
            {
                Vector3 eulerAngles = transform.eulerAngles;
                eulerAngles.x = 90.0f;
                transform.eulerAngles = eulerAngles;
                orthographicCameraControls();
            }
            else
            {
                perspectiveCameraControls();
            }
        }
    }

    private void perspectiveCameraControls()
    {
        float strafe = Input.GetAxis("Horizontal") * strafeSpd * 0.02f;
        float vertical = Input.GetAxis("Vertical") * strafeSpd * 0.02f;
        Vector2 scrollDelta = Input.mouseScrollDelta;
        distance = scrollDelta.y * zoomSpd * 0.05f;

        handleMouseRotation();

        Vector2 strafeVec = handleMouseStrafe();

        Quaternion rotation = Quaternion.Euler(y, x, 0.0f);
        Vector3 position = transform.position;
        if (Camera.main.orthographic)
        {
            rotation = Quaternion.Euler(90, -90, 0.0f);
            position = new Vector3(strafe + strafeVec.x * 0.02f, vertical + strafeVec.y * 0.02f, distance) + target.position;
        }
        else
        {
            position = rotation * new Vector3(strafe + strafeVec.x * 0.02f, vertical + strafeVec.y * 0.02f, distance) + target.position;
        }
        
        transform.rotation = rotation;
        transform.position = position;
    }

    private void orthographicCameraControls()
    {
        Vector2 scrollDelta = Input.mouseScrollDelta;
        float orthoSize = Camera.main.orthographicSize;
        orthoSize -= scrollDelta.y * 0.23f;

        float verticalSpeed = Input.GetAxis("Zoom");
        orthoSize -= verticalSpeed * 0.3f;

        orthoSize = Mathf.Clamp(orthoSize, minOrthoSize, maxOrthoSize);
        Camera.main.orthographicSize = orthoSize;


        float normSize = (orthoSize/* - minOrthoSize*/) / (maxOrthoSize - minOrthoSize);

        float sidewaysSpeed = Input.GetAxis("Horizontal") * strafeSpd * normSize * 0.1f;
        float forwardSpeed = Input.GetAxis("Vertical") * strafeSpd * normSize * 0.1f;      

        Vector3 position = transform.position;
        Vector3 forward = transform.up;
        Vector3 right = transform.right;

        position += forward * forwardSpeed + right * sidewaysSpeed;


        float yawSpeed = Input.GetAxis("Horizontal2");

        if(didSnapRotation == false
            && yawSpeed != 0.0f)
        {
            if (Input.GetKey(KeyCode.LeftControl)
            || Input.GetKey(KeyCode.RightControl))
            {
                float yAngle = transform.eulerAngles.y;

                yAngle = yAngle % 90.0f;

                if (yawSpeed > 0.0f)
                {
                    yawSpeed = -yAngle;
                    if (yawSpeed >= 0.0f)
                    {
                        yawSpeed = -90.0f;
                    }
                }
                else if(yawSpeed < 0.0f)
                {
                    yawSpeed = 90.0f - yAngle;
                }

                transform.Rotate(0.0f, 0.0f, -yawSpeed);

                didSnapRotation = true;
            }
            else
            {
                transform.Rotate(0.0f, 0.0f, yawSpeed);
            }
        }
        else if (yawSpeed == 0.0f)
        {
            didSnapRotation = false;
        }


        Vector2 strafeVec = handleMouseStrafe();
        position += forward * (strafeVec.y * 0.07f) + right * (strafeVec.x * 0.07f);
        transform.position = position;

        Vector2 rotVec = handleMouseRotation();
        transform.Rotate(0.0f, 0.0f, rotVec.y + rotVec.x);
    }

    private Vector2 handleMouseStrafe()
    {
        Vector2 result = new Vector2(0, 0);

        if (Input.GetMouseButtonDown(2))
        {
            lastnx = Input.mousePosition.x;
            lastny = Input.mousePosition.y;
            strf = true;
        }

        if (Input.GetMouseButtonUp(2))
        {
            strf = false;
        }

        if (strf)
        {
            float curx = Input.mousePosition.x;
            float dx = curx - lastnx;
            result.x += dx;

            float cury = Input.mousePosition.y;
            float dy = cury - lastny;
            result.y += dy;

            lastnx = curx;
            lastny = cury;
        }

        return result;
    }

    private Vector2 handleMouseRotation()
    {
        Vector2 result = new Vector2(0, 0);

        if (Input.GetMouseButtonDown(1))
        {
            lastmx = Input.mousePosition.x;
            lastmy = Input.mousePosition.y;
        }

        if (Input.GetMouseButton(1))
        {
            float curx = Input.mousePosition.x;
            float dx = curx - lastmx;
            result.x = (float)dx * xSpeed;
            x += (float)dx * xSpeed;

            float cury = Input.mousePosition.y;
            float dy = cury - lastmy;
            result.y = (float)dy * ySpeed;
            y -= (float)dy * ySpeed;

            lastmx = curx;
            lastmy = cury;
        }

        return result;
    }

    public static float ClampAngle (float angle, float min, float max)
    {
        if (angle < -360.0f)
            angle += 360.0f;
        if (angle > 360.0f)
            angle -= 360.0f;
        return Mathf.Clamp (angle, min, max);
    }
}