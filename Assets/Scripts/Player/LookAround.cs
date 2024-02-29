using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct View 
{
    public Vector3 loc;
    public Vector3 rot;

    public View(Vector3 inLoc, Vector3 inRot) { loc = inLoc; rot = inRot; }
}


public class LookAround : NetworkBehaviour
{
    public float mouseXSensitivity = 100f;
    public float mouseYSensitivity = 1f;
    public Transform playerBody;
    private View TopDownView = new View(new Vector3(0, 3, -2), new Vector3(52, 0, 0));
    private View StraightView = new View(new Vector3(0, 2.23f, -4.18f), new Vector3(10, 0, 0));
    private View DownTopView = new View(new Vector3(0, -0.27f, -2), new Vector3(-70, 0, 0));
    private float yRotation = 0.3f;

    // Start is called before the first frame update
    void Start()
    {
        if (!authority) return;

        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (!authority) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity * Time.deltaTime;
        float mouseY = -1 * Input.GetAxis("Mouse Y") * mouseYSensitivity * Time.deltaTime;

        yRotation = Mathf.Clamp(yRotation + mouseY, 0, 1);

        Vector3 pos;
        if (yRotation <= 0.5)
            pos = new Vector3(
                Mathf.Lerp(DownTopView.loc.x, StraightView.loc.x, yRotation * 2),
                Mathf.Lerp(DownTopView.loc.y, StraightView.loc.y, yRotation * 2),
                Mathf.Lerp(DownTopView.loc.z, StraightView.loc.z, yRotation * 2)
            );
        else
            pos = new Vector3(
                Mathf.Lerp(StraightView.loc.x, TopDownView.loc.x, (yRotation - 0.5f) * 2),
                Mathf.Lerp(StraightView.loc.y, TopDownView.loc.y, (yRotation - 0.5f) * 2),
                Mathf.Lerp(StraightView.loc.z, TopDownView.loc.z, (yRotation - 0.5f) * 2)
            );

        Quaternion rot;
        if (yRotation <= 0.5) 
            rot = Quaternion.Euler(new Vector3(
                Mathf.Lerp(DownTopView.rot.x, StraightView.rot.x, yRotation * 2),
                Mathf.Lerp(DownTopView.rot.y, StraightView.rot.y, yRotation * 2),
                Mathf.Lerp(DownTopView.rot.z, StraightView.rot.z, yRotation * 2)
            ));
        else
            rot = Quaternion.Euler(new Vector3(
                Mathf.Lerp(StraightView.rot.x, TopDownView.rot.x, (yRotation - 0.5f) * 2),
                Mathf.Lerp(StraightView.rot.y, TopDownView.rot.y, (yRotation - 0.5f) * 2),
                Mathf.Lerp(StraightView.rot.z, TopDownView.rot.z, (yRotation - 0.5f) * 2)
            ));

        transform.localPosition = pos;
        transform.localRotation = rot;

        //X Rotation
        playerBody.Rotate(Vector3.up * mouseX);
    }
}